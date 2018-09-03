//  Licensed under the MIT license. For details, see LICENSE.md.

import Cocoa

extension NSView {

    @IBInspectable var backgroundColor: NSColor? {
        get {
            if let color = layer?.backgroundColor {
                return NSColor(cgColor: color)
            } else {
                return nil
            }
        }
        set {
            if layer == nil {
                wantsLayer = true
            }
            layer!.backgroundColor = newValue?.cgColor ?? nil
        }
    }
}
