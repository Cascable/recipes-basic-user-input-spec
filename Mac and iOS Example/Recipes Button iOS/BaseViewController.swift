//
//  ViewController.swift
//  Recipes Button
//
//  Created by Daniel Kennett on 2018-08-02.
//  Copyright © 2018 Cascable AB. All rights reserved.
//

import UIKit

enum BLEUIState {
    case notAvailable
    case advertising
    case connected
}

extension BasicUserInputDevice {
    var uiState: BLEUIState {
        if !isAvailable || !isAdvertising {
            return .notAvailable
        } else if !isConnected {
            return .advertising
        } else {
            return .connected
        }
    }
}

class BaseViewController: UIViewController {

    @IBOutlet var titleLabel: UILabel!
    @IBOutlet var messageLabel: UILabel!
    @IBOutlet var helpButton: UIButton!

    var observations = [NSKeyValueObservation]()
    let ble = BasicUserInputDevice()

    override func viewDidLoad() {
        super.viewDidLoad()

        observations.append(ble.observe(\.isAvailable, options: [.initial]) { (ble, change) in
            self.bleDidChangeAvailability()
        })

        observations.append(ble.observe(\.isAdvertising, options: [.initial]) { (ble, change) in
            self.bleDidChangeAvailability()
        })

        observations.append(ble.observe(\.isConnected, options: [.initial]) { (ble, change) in
            self.bleDidChangeAvailability()
            self.bleDidChangeConnectedness()
        })
    }

    @IBAction func helpButtonPressed(_ sender: UIButton) {
        let url = URL(string: "https://services.cascable.se/go?location=recipes-button-client-help")!
        UIApplication.shared.open(url, options: [:], completionHandler: nil)
    }

    // MARK: - State Change Handlers

    private func bleDidChangeAvailability() {
        switch ble.uiState {
        case .notAvailable:
            titleLabel.text = NSLocalizedString("BLENotAvailableTitle", comment: "")
            messageLabel.text = String(format: NSLocalizedString("BLENotAvailableMessage", comment: ""),
                                       UIDevice.current.localizedModel)
        case .advertising, .connected:
            titleLabel.text = NSLocalizedString("BLEWaitingForConnectionTitle", comment: "")
            messageLabel.text = String(format: NSLocalizedString("BLEWaitingForConnectionMessage", comment: ""),
                                       UIDevice.current.localizedModel)
        }
    }

    private func bleDidChangeConnectedness() {
        if ble.isConnected {
            guard presentedViewController == nil else { return }
            performSegue(withIdentifier: "showConnected", sender: nil)
        } else {
            presentedViewController?.dismiss(animated: true, completion: nil)
        }
    }

    override func prepare(for segue: UIStoryboardSegue, sender: Any?) {
        if let destination = segue.destination as? RemoteInputViewController {
            destination.ble = ble
        }
    }

}

