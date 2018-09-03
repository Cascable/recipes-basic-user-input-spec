//
//  PillButton.swift
//  Recipes Button iOS
//
//  Created by Daniel Kennett on 2018-08-25.
//  Copyright © 2018 Cascable AB. All rights reserved.
//  Licensed under the MIT license. For details, see LICENSE.md.
//

import UIKit

@IBDesignable class PillButton: UIButton {

    override init(frame: CGRect) {
        super.init(frame: frame)
        setup()
    }

    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
        setup()
    }

    override func layoutSubviews() {
        super.layoutSubviews()
        layer.cornerRadius = min(frame.height, frame.width) / 2.0
        layer.masksToBounds = true
    }

    override func prepareForInterfaceBuilder() {
        super.prepareForInterfaceBuilder()
        setup()
    }

    private func setup() {
        backgroundColor = Colors.tintColor
        setTitleColor(.white, for: .normal)
    }

}
