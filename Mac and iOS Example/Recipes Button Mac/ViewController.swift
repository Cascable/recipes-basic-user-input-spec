//
//  ViewController.swift
//  Recipes Button Mac
//
//  Created by Daniel Kennett on 2018-08-02.
//  Copyright © 2018 Cascable AB. All rights reserved.
//  Licensed under the MIT license. For details, see LICENSE.md.
//

import AppKit
import Cocoa

class ViewController: NSViewController {

    @objc let service = BasicUserInputDevice()
    @IBOutlet weak var messageLabel: NSTextField!
    @IBOutlet var bleUnavailableView: NSView!
    @IBOutlet var waitingForConnectionView: NSView!
    var observations = [NSKeyValueObservation]()

    override func viewDidLoad() {
        super.viewDidLoad()

        bleUnavailableView.translatesAutoresizingMaskIntoConstraints = false
        waitingForConnectionView.translatesAutoresizingMaskIntoConstraints = false

        view.addSubview(bleUnavailableView)
        view.addSubview(waitingForConnectionView)

        view.addConstraints(NSLayoutConstraint.constraintsForFillingSuperView(with: bleUnavailableView))
        view.addConstraints(NSLayoutConstraint.constraintsForFillingSuperView(with: waitingForConnectionView))

        observations.append(service.observe(\.userMessage) { device, change in
            if let message = device.userMessage, message.count > 0 {
                self.messageLabel.stringValue = message
            } else {
                self.messageLabel.stringValue = "Push the button to continue."
            }
        })

        observations.append(service.observe(\.isAvailable) { _, _ in
            self.updateViewState()
        })

        observations.append(service.observe(\.isAdvertising) { _, _ in
            self.updateViewState()
        })

        observations.append(service.observe(\.isConnected) { _, _ in
            self.updateViewState()
        })

        updateViewState()
    }

    func updateViewState() {
        if !service.isAvailable || !service.isAdvertising {
            bleUnavailableView.isHidden = false
            waitingForConnectionView.isHidden = true
        } else if !service.isConnected {
            bleUnavailableView.isHidden = true
            waitingForConnectionView.isHidden = false
        } else {
            bleUnavailableView.isHidden = true
            waitingForConnectionView.isHidden = true
        }
    }

    @IBAction func continueClicked(_ sender: Any) {
        // Mac buttons don't have fine-grained events like iOS ones, so synthesize a down-up.
        service.continueButtonState = .down
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
            self.service.continueButtonState = .up
        }
    }

    @IBAction func stopClicked(_ sender: Any) {
        guard service.canStopExecution else {
            NSSound.beep()
            return
        }

        // Mac buttons don't have fine-grained events like iOS ones, so synthesize a down-up.
        service.stopButtonState = .down
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
            self.service.stopButtonState = .up
        }
    }

}

