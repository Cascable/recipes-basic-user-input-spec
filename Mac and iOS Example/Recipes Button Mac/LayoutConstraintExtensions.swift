//  Licensed under the MIT license. For details, see LICENSE.md.

import Cocoa

extension NSLayoutConstraint {
    class func constraintsForFillingSuperView(with view: NSView) -> [NSLayoutConstraint] {
        return constraintsForFillingSuperView(with: view, insets: NSEdgeInsets(top: 0.0, left: 0.0, bottom: 0.0, right: 0.0))
    }

    class func constraintsForFillingSuperView(with view: NSView, insets: NSEdgeInsets) -> [NSLayoutConstraint] {
        var constraints = [NSLayoutConstraint]()
        let metrics = ["top": insets.top as NSNumber, "bottom": insets.bottom as NSNumber,
                       "left": insets.left as NSNumber, "right": insets.right as NSNumber]

        constraints.append(contentsOf: self.constraints(withVisualFormat: "|-(left)-[view]-(right)-|",
                                                        options: .directionLeadingToTrailing,
                                                        metrics: metrics,
                                                        views: ["view": view]))

        constraints.append(contentsOf: self.constraints(withVisualFormat: "V:|-(top)-[view]-(bottom)-|",
                                                        options: .directionLeadingToTrailing,
                                                        metrics: metrics,
                                                        views: ["view": view]))

        return constraints
    }
}
