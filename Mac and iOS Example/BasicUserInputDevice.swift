//
//  BasicUserInputDevice.swift
//  Recipes Button
//
//  Created by Daniel Kennett on 2018-08-02.
//  Copyright © 2018 Cascable AB. All rights reserved.
//  Licensed under the MIT license. For details, see LICENSE.md.
//

import CoreBluetooth
import Foundation

#if os(iOS)
import UIKit
#endif

extension CBUUID {
    static let cascableBasicUserInputService = CBUUID(string: "9A43142A-F619-41FF-945D-527023EF5B9D")
    static let continueButtonDownCharacteristic = CBUUID(string: "EB366CE4-1952-4A55-A319-7E27A865554A")
    static let stopButtonDownCharacteristic = CBUUID(string: "A83173E8-9546-4D91-A736-B087AE514321")
    static let remoteCanContinueCharacteristic = CBUUID(string: "941680A1-A4D9-48DF-927B-2E19870F8085")
    static let remoteCanStopCharacteristic = CBUUID(string: "FD64C111-356F-4968-85C2-2DD979E3878E")
    static let userMessageCharacteristic = CBUUID(string: "DEFF81CC-0F74-4E86-BA01-F5D34E1C33E6")
}

fileprivate extension FixedWidthInteger {
    var dataValue: Data {
        var leValue = littleEndian
        return Data(buffer: UnsafeBufferPointer(start: &leValue, count: 1))
    }
}

fileprivate extension Data {
    func read<T: FixedWidthInteger>(_ type: T.Type, at offset: Int) -> T? {
        let typeSize = T.bitWidth / 8
        if offset + typeSize > count {
            return nil
        }

        let bits = withUnsafeBytes({ (bytePointer: UnsafePointer<UInt8>) -> T in
            bytePointer.advanced(by: offset).withMemoryRebound(to: type, capacity: 1, { pointer in
                pointer.pointee
            })
        })
        return T(littleEndian: bits)
    }
}

@objc class BasicUserInputDevice: NSObject, CBPeripheralManagerDelegate {

    @objc enum ButtonState: UInt8 {
        case up = 0x00
        case down = 0x01
        case cancelled = 0x02
    }

    private var manager: CBPeripheralManager!

    override init() {
        super.init()
        manager = CBPeripheralManager(delegate: self, queue: .main)
    }

    deinit {
        stopServices()
    }

    // MARK: - Public Properties

    @objc dynamic var continueButtonState: ButtonState = .up {
        didSet {
            notifySubscribersOfButtonState(continueButtonState, for: continueButtonDownCharacteristic)
        }
    }

    @objc dynamic var stopButtonState: ButtonState = .up {
        didSet {
            notifySubscribersOfButtonState(stopButtonState, for: stopButtonDownCharacteristic)
        }
    }

    @objc dynamic private(set) var userMessage: String? = nil
    @objc dynamic private(set) var canContinueExecution: Bool = false
    @objc dynamic private(set) var canStopExecution: Bool = false

    @objc dynamic private(set) var isAvailable: Bool = false
    @objc dynamic private(set) var isAdvertising: Bool = false
    @objc dynamic private(set) var isConnected: Bool = false

    // MARK: - BLE

    private var continueButtonDownCharacteristic: CBMutableCharacteristic?
    private var stopButtonDownCharacteristic: CBMutableCharacteristic?
    private var remoteCanContinueCharacteristic: CBMutableCharacteristic?
    private var remoteCanStopCharacteristic: CBMutableCharacteristic?
    private var userMessageCharacteristic: CBMutableCharacteristic?
    private var basicInputService: CBService?

    private var needsRetransmitOfNotifications: Bool = false

    private func setupServices() {

        stopServices()

        let continueButton = CBMutableCharacteristic(type: .continueButtonDownCharacteristic,
                                                     properties: [.read, .notify], value: nil, permissions: .readable)

        let stopButton = CBMutableCharacteristic(type: .stopButtonDownCharacteristic,
                                                 properties: [.read, .notify], value: nil, permissions: .readable)

        let remoteCanStop = CBMutableCharacteristic(type: .remoteCanStopCharacteristic,
                                                    properties: [.write, .writeWithoutResponse], value: nil, permissions: .writeable)

        let remoteCanContinue = CBMutableCharacteristic(type: .remoteCanContinueCharacteristic,
                                                        properties: [.write, .writeWithoutResponse], value: nil, permissions: .writeable)

        let userMessage = CBMutableCharacteristic(type: .userMessageCharacteristic,
                                                  properties: [.write, .writeWithoutResponse], value: nil, permissions: .writeable)

        let service = CBMutableService(type: .cascableBasicUserInputService, primary: true)
        service.characteristics = [continueButton, stopButton, remoteCanContinue, remoteCanStop, userMessage]
        manager.add(service)

        continueButtonDownCharacteristic = continueButton
        stopButtonDownCharacteristic = stopButton
        remoteCanContinueCharacteristic = remoteCanContinue
        remoteCanStopCharacteristic = remoteCanStop
        userMessageCharacteristic = userMessage
        basicInputService = service

        #if os(iOS)
        let name = UIDevice.current.name
        #elseif os(OSX)
        let name = Host.current().localizedName ?? "Mac"
        #endif

        manager.startAdvertising([CBAdvertisementDataServiceUUIDsKey : [CBUUID.cascableBasicUserInputService],
                                  CBAdvertisementDataLocalNameKey : "Cascable Button on \(name)"])
    }

    private func stopServices() {
        isAdvertising = false
        isConnected = false
        canStopExecution = false
        canContinueExecution = false
        subscribedCentrals.removeAll()
        manager.removeAllServices()
        if manager.isAdvertising {
            manager.stopAdvertising()
            print("Services stopped")
        }

        continueButtonDownCharacteristic = nil
        stopButtonDownCharacteristic = nil
        remoteCanContinueCharacteristic = nil
        remoteCanStopCharacteristic = nil
        userMessageCharacteristic = nil
        basicInputService = nil
    }

    private func notifySubscribersOfButtonState(_ state: ButtonState, for characteristic: CBMutableCharacteristic?) {
        guard manager.state == .poweredOn, let characteristic = characteristic else { return }
        if !manager.updateValue(state.rawValue.dataValue, for: characteristic, onSubscribedCentrals: nil) {
            needsRetransmitOfNotifications = true
        } else {
            needsRetransmitOfNotifications = false
        }
    }

    // MARK: - CBPeripheralManagerDelegate (Read and Write)

    func peripheralManager(_ peripheral: CBPeripheralManager, didReceiveRead request: CBATTRequest) {

        peripheral.setDesiredConnectionLatency(.low, for: request.central)

        guard request.offset > 0 else {
            // Our readable characteristics are only a byte long.
            peripheral.respond(to: request, withResult: .invalidOffset)
            return
        }

        switch request.characteristic.uuid {
        case .continueButtonDownCharacteristic:
            request.value = continueButtonState.rawValue.dataValue
            peripheral.respond(to: request, withResult: .success)
        case .stopButtonDownCharacteristic:
            request.value = stopButtonState.rawValue.dataValue
            peripheral.respond(to: request, withResult: .success)
        case .remoteCanContinueCharacteristic:
            peripheral.respond(to: request, withResult: .readNotPermitted)
        case .remoteCanStopCharacteristic:
            peripheral.respond(to: request, withResult: .readNotPermitted)
        case .userMessageCharacteristic:
            peripheral.respond(to: request, withResult: .readNotPermitted)
        default:
            peripheral.respond(to: request, withResult: .requestNotSupported)
        }
    }

    func peripheralManager(_ peripheral: CBPeripheralManager, didReceiveWrite requests: [CBATTRequest]) {

        var messageBuffer = Data()

        for request in requests {
            if request.characteristic.uuid != .remoteCanStopCharacteristic &&
                request.characteristic.uuid != .remoteCanContinueCharacteristic &&
                request.characteristic.uuid != .userMessageCharacteristic {
                peripheral.respond(to: requests.first!, withResult: .writeNotPermitted)
                return
            }

            if request.characteristic.uuid == .remoteCanStopCharacteristic ||
                request.characteristic.uuid == .remoteCanContinueCharacteristic {
                if request.offset > 0 {
                    peripheral.respond(to: requests.first!, withResult: .invalidOffset)
                    return
                }

                guard let value = request.value, value.count == 1 else {
                    peripheral.respond(to: requests.first!, withResult: .invalidAttributeValueLength)
                    return
                }

                if let intValue = value.read(UInt8.self, at: 0) {
                    if request.characteristic.uuid == .remoteCanStopCharacteristic {
                        canStopExecution = intValue > 0
                    } else {
                        canContinueExecution = intValue > 0
                    }
                }
            }

            if request.characteristic.uuid == .userMessageCharacteristic {
                guard let value = request.value else {
                    peripheral.respond(to: requests.first!, withResult: .invalidAttributeValueLength)
                    return
                }
                messageBuffer.insert(contentsOf: value, at: request.offset)
            }
        }

        if messageBuffer.count > 0, let message = String(bytes: messageBuffer, encoding: .utf8) {
            userMessage = message
        }

        peripheral.setDesiredConnectionLatency(.low, for: requests.first!.central)
        peripheral.respond(to: requests.first!, withResult: .success)
    }

    func peripheralManagerIsReady(toUpdateSubscribers peripheral: CBPeripheralManager) {
        if needsRetransmitOfNotifications {
            notifySubscribersOfButtonState(continueButtonState, for: continueButtonDownCharacteristic)
            notifySubscribersOfButtonState(stopButtonState, for: stopButtonDownCharacteristic)
        }
    }

    // MARK: - CBPeripheralManagerDelegate (Other)

    func peripheralManagerDidUpdateState(_ peripheral: CBPeripheralManager) {
        switch manager.state {
        case .unknown: stopServices()
        case .resetting: stopServices()
        case .unsupported: stopServices()
        case .unauthorized: stopServices()
        case .poweredOff: stopServices()
        case .poweredOn: setupServices()
        }
        isAvailable = (manager.state == .poweredOn)
    }

    func peripheralManagerDidStartAdvertising(_ peripheral: CBPeripheralManager, error: Error?) {
        isAdvertising = (error == nil)
    }

    var subscribedCentrals = [CBCentral]()

    func peripheralManager(_ peripheral: CBPeripheralManager, central: CBCentral, didSubscribeTo characteristic: CBCharacteristic) {
        peripheral.setDesiredConnectionLatency(.low, for: central)
        if characteristic.uuid == .continueButtonDownCharacteristic && !subscribedCentrals.contains(central) {
            subscribedCentrals.append(central)
        }
        isConnected = (subscribedCentrals.count > 0)
    }

    func peripheralManager(_ peripheral: CBPeripheralManager, central: CBCentral, didUnsubscribeFrom characteristic: CBCharacteristic) {
        if characteristic.uuid == .continueButtonDownCharacteristic, let index = subscribedCentrals.index(of: central) {
            subscribedCentrals.remove(at: index)
        }
        isConnected = (subscribedCentrals.count > 0)
    }

}

