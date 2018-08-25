//
//  RemoteInputViewController.swift
//  Recipes Button iOS
//
//  Created by Daniel Kennett on 2018-08-25.
//  Copyright © 2018 Cascable AB. All rights reserved.
//

import UIKit

class RemoteInputViewController: UIViewController {

    var observations = [NSKeyValueObservation]()
    var ble: BasicUserInputDevice!

    @IBOutlet var recipeStatusLabel: UILabel!
    @IBOutlet var userMessageLabel: UILabel!
    @IBOutlet var stopButton: UIButton!
    @IBOutlet var continueButton: UIButton!

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)

        observations.append(ble.observe(\.userMessage, options: [.initial]) { ble, _ in
            guard let message = ble.userMessage, message.count > 0 else {
                self.userMessageLabel.text = NSLocalizedString("DefaultUserMessage", comment: "")
                return
            }
            self.userMessageLabel.text = message
        })

        observations.append(ble.observe(\.canStopExecution, options: [.initial]) { ble, _ in
            self.stopButton.isHidden = !ble.canStopExecution
        })

        observations.append(ble.observe(\.canContinueExecution, options: [.initial]) { ble, _ in
            self.continueButton.isEnabled = ble.canContinueExecution
            self.continueButton.alpha = ble.canContinueExecution ? 1.0 : 0.5

            if ble.canContinueExecution {
                self.recipeStatusLabel.text = NSLocalizedString("RecipeWaitingStateTitle", comment: "")
                let haptic = UINotificationFeedbackGenerator()
                haptic.notificationOccurred(.success)
            } else {
                self.recipeStatusLabel.text = NSLocalizedString("RecipeRunningStateTitle", comment: "")
            }
        })
    }

    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)
        observations.forEach({ $0.invalidate() })
        observations.removeAll()
    }

    // MARK: - Button State Changes

    @IBAction func continueButtonDown(_ sender: UIButton) {
        ble.continueButtonState = .down
    }

    @IBAction func continueButtonUpInside(_ sender: UIButton) {
        ble.continueButtonState = .up
    }

    @IBAction func continueButtonUpOutside(_ sender: UIButton) {
        ble.continueButtonState = .cancelled
    }

    @IBAction func stopButtonDown(_ sender: UIButton) {
        ble.stopButtonState = .down
    }

    @IBAction func stopButtonUpInside(_ sender: UIButton) {
        ble.stopButtonState = .up
    }

    @IBAction func stopButtonUpOutside(_ sender: UIButton) {
        ble.stopButtonState = .cancelled
    }

}
