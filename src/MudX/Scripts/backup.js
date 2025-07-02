// updates from MudBlazor

window.mudpopoverHelper.calculatePopoverPosition =
    function (list, boundingRect, selfRect) {
        let top = boundingRect.top;     // default for mud-popover-anchor-top-left
        let left = boundingRect.left;   // default for mud-popover-anchor-top-left

        const isPositionOverride = list.indexOf('mud-popover-position-override') >= 0;

        let offsetX = 0;
        let offsetY = 0;
        // transform origin

        if (list.indexOf('mud-popover-top-left') >= 0) {
            offsetX = 0;
            offsetY = 0;
        } else if (list.indexOf('mud-popover-top-center') >= 0) {
            offsetX = -selfRect.width / 2;
            offsetY = 0;
        } else if (list.indexOf('mud-popover-top-right') >= 0) {
            offsetX = -selfRect.width;
            offsetY = 0;
        }

        else if (list.indexOf('mud-popover-center-left') >= 0) {
            offsetX = 0;
            offsetY = -selfRect.height / 2;
        } else if (list.indexOf('mud-popover-center-center') >= 0) {
            offsetX = -selfRect.width / 2;
            offsetY = -selfRect.height / 2;
        } else if (list.indexOf('mud-popover-center-right') >= 0) {
            offsetX = -selfRect.width;
            offsetY = -selfRect.height / 2;
        }

        else if (list.indexOf('mud-popover-bottom-left') >= 0) {
            offsetX = 0;
            offsetY = -selfRect.height;
        } else if (list.indexOf('mud-popover-bottom-center') >= 0) {
            offsetX = -selfRect.width / 2;
            offsetY = -selfRect.height;
        } else if (list.indexOf('mud-popover-bottom-right') >= 0) {
            offsetX = -selfRect.width;
            offsetY = -selfRect.height;
        }

        if (!isPositionOverride) {
            // anchor origin, don't flip anchors on position override
            if (list.indexOf('mud-popover-anchor-top-left') >= 0) {
                left = boundingRect.left;
                top = boundingRect.top;
            } else if (list.indexOf('mud-popover-anchor-top-center') >= 0) {
                left = boundingRect.left + boundingRect.width / 2;
                top = boundingRect.top;
            } else if (list.indexOf('mud-popover-anchor-top-right') >= 0) {
                left = boundingRect.left + boundingRect.width;
                top = boundingRect.top;

            } else if (list.indexOf('mud-popover-anchor-center-left') >= 0) {
                left = boundingRect.left;
                top = boundingRect.top + boundingRect.height / 2;
            } else if (list.indexOf('mud-popover-anchor-center-center') >= 0) {
                left = boundingRect.left + boundingRect.width / 2;
                top = boundingRect.top + boundingRect.height / 2;
            } else if (list.indexOf('mud-popover-anchor-center-right') >= 0) {
                left = boundingRect.left + boundingRect.width;
                top = boundingRect.top + boundingRect.height / 2;

            } else if (list.indexOf('mud-popover-anchor-bottom-left') >= 0) {
                left = boundingRect.left;
                top = boundingRect.top + boundingRect.height;
            } else if (list.indexOf('mud-popover-anchor-bottom-center') >= 0) {
                left = boundingRect.left + boundingRect.width / 2;
                top = boundingRect.top + boundingRect.height;
            } else if (list.indexOf('mud-popover-anchor-bottom-right') >= 0) {
                left = boundingRect.left + boundingRect.width;
                top = boundingRect.top + boundingRect.height;
            }
        }
        return {
            top: top, left: left, offsetX: offsetX, offsetY: offsetY, anchorY: top, anchorX: left
        };
    };

// primary positioning method
window.mudpopoverHelper.placePopover =
    function (popoverNode, classSelector) {
        // parentNode is the calling element, mudmenu/tooltip/etc not the parent popover if it's a child popover
        // this happens at page load unless it's popover inside a popover, then it happens when you activate the parent

        if (popoverNode && popoverNode.parentNode) {
            const id = popoverNode.id.substr(8);
            const popoverContentNode = document.getElementById('popovercontent-' + id);

            // if the popover doesn't exist we stop
            if (!popoverContentNode) return;

            const classList = popoverContentNode.classList;

            // if the popover isn't open we stop
            if (!classList.contains('mud-popover-open')) return;

            // if a classSelector was supplied and doesn't exist we stop
            if (classSelector && !classList.contains(classSelector)) return;

            // Batch DOM reads
            let boundingRect = popoverNode.parentNode.getBoundingClientRect();
            if (!window.mudpopoverHelper.isInViewport(popoverNode, boundingRect)) {
                // if the parentNode isn't visible at all we stop
                return;
            }
            const selfRect = popoverContentNode.getBoundingClientRect();
            const popoverNodeStyle = window.getComputedStyle(popoverNode);
            const isPositionFixed = popoverNodeStyle.position === 'fixed';
            const isPositionOverride = classList.contains('mud-popover-position-override');
            const isRelativeWidth = classList.contains('mud-popover-relative-width');
            const isAdaptiveWidth = classList.contains('mud-popover-adaptive-width');
            const isFlipOnOpen = classList.contains('mud-popover-overflow-flip-onopen');
            const isFlipAlways = classList.contains('mud-popover-overflow-flip-always');
            const zIndexAuto = popoverNodeStyle.getPropertyValue('z-index') === 'auto';
            const classListArray = Array.from(classList);

            if (isPositionOverride) {
                const positiontop = parseInt(popoverContentNode.getAttribute('data-pc-y')) || boundingRect.top;
                const positionleft = parseInt(popoverContentNode.getAttribute('data-pc-x')) || boundingRect.left;
                const scrollLeft = window.scrollX;
                const scrollTop = window.scrollY;

                // bounding rect for flipping
                boundingRect = {
                    left: positionleft - scrollLeft,
                    top: positiontop - scrollTop,
                    right: positionleft + 1,
                    bottom: positiontop + 1,
                    width: 1,
                    height: 1
                };
            }

            // calculate position based on opening anchor/transform
            const position = window.mudpopoverHelper.calculatePopoverPosition(classListArray, boundingRect, selfRect);
            let left = position.left; // X-coordinate of the popover
            let top = position.top; // Y-coordinate of the popover
            let offsetX = position.offsetX; // Horizontal offset of the popover
            let offsetY = position.offsetY; // Vertical offset of the popover
            let anchorY = position.anchorY; // Y-coordinate of the opening anchor
            let anchorX = position.anchorX; // X-coordinate of the opening anchor

            // reset widths and allow them to be changed after initial creation
            popoverContentNode.style['max-width'] = 'none';
            popoverContentNode.style['min-width'] = 'none';
            if (isRelativeWidth) {
                popoverContentNode.style['max-width'] = (boundingRect.width) + 'px';
            }
            else if (isAdaptiveWidth) {
                popoverContentNode.style['min-width'] = (boundingRect.width) + 'px';
            }

            // Reset max-height if it was previously set and anchor is in bounds
            if (popoverContentNode.mudHeight && anchorY > 0 && anchorY < window.innerHeight) {
                popoverContentNode.style.maxHeight = null;
                popoverContentNode.mudHeight = null;
            }

            // flipping logic
            if (isFlipOnOpen || isFlipAlways) {

                const appBarElements = document.getElementsByClassName("mud-appbar mud-appbar-fixed-top");
                let appBarOffset = 0;
                if (appBarElements.length > 0) {
                    appBarOffset = appBarElements[0].getBoundingClientRect().height;
                }

                // mudPopoverFliped is the flip direction for first flip on flip - onopen popovers
                let selector = popoverContentNode.mudPopoverFliped;

                // flip routine off transform origin, sets selector to an axis to flip on if needed
                if (!selector) {
                    const popoverHeight = popoverContentNode.offsetHeight;
                    const popoverWidth = popoverContentNode.offsetWidth;
                    // For mud-popover-top-left

                    if (classList.contains('mud-popover-top-left')) {
                        // Space available in current direction
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin; // Space below the anchor
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin; // Space to the right of the anchor

                        // Space available in opposite direction
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;

                        // Check if popover exceeds available space AND if opposite side has more space
                        const shouldFlipVertical = popoverHeight > spaceBelow && spaceAbove > spaceBelow;
                        const shouldFlipHorizontal = popoverWidth > spaceRight && spaceLeft > spaceRight;
                        // Apply flips based on space comparisons
                        if (shouldFlipVertical && shouldFlipHorizontal) {
                            selector = 'top-and-left';
                        }
                        else if (shouldFlipVertical) {
                            selector = 'top';
                        }
                        else if (shouldFlipHorizontal) {
                            selector = 'left';
                        }
                    }

                    // For mud-popover-top-center
                    else if (classList.contains('mud-popover-top-center')) {
                        // Space available in current direction vs opposite direction
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;

                        // Only flip if popover exceeds available space AND there's more space in opposite direction
                        if (popoverHeight > spaceBelow && spaceAbove > spaceBelow) {
                            selector = 'top';
                        }
                    }

                    // For mud-popover-top-right
                    else if (classList.contains('mud-popover-top-right')) {
                        // Space available in current direction
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;

                        // Space available in opposite direction
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin;

                        // Check if popover exceeds available space AND if opposite side has more space
                        const shouldFlipVertical = popoverHeight > spaceBelow && spaceAbove > spaceBelow;
                        const shouldFlipHorizontal = popoverWidth > spaceLeft && spaceRight > spaceLeft;

                        if (shouldFlipVertical && shouldFlipHorizontal) {
                            selector = 'top-and-right';
                        }
                        else if (shouldFlipVertical) {
                            selector = 'top';
                        }
                        else if (shouldFlipHorizontal) {
                            selector = 'right';
                        }
                    }

                    // For mud-popover-center-left
                    else if (classList.contains('mud-popover-center-left')) {
                        // Space available in current vs opposite direction
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin;
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;

                        if (popoverWidth > spaceRight && spaceLeft > spaceRight) {
                            selector = 'left';
                        }
                    }

                    // For mud-popover-center-right
                    else if (classList.contains('mud-popover-center-right')) {
                        // Space available in current vs opposite direction
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin;

                        if (popoverWidth > spaceLeft && spaceRight > spaceLeft) {
                            selector = 'right';
                        }
                    }

                    // For mud-popover-bottom-left
                    else if (classList.contains('mud-popover-bottom-left')) {
                        // Space available in current direction
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin;

                        // Space available in opposite direction
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;

                        // Check if popover exceeds available space AND if opposite side has more space
                        const shouldFlipVertical = popoverHeight > spaceAbove && spaceBelow > spaceAbove;
                        const shouldFlipHorizontal = popoverWidth > spaceRight && spaceLeft > spaceRight;

                        if (shouldFlipVertical && shouldFlipHorizontal) {
                            selector = 'bottom-and-left';
                        }
                        else if (shouldFlipVertical) {
                            selector = 'bottom';
                        }
                        else if (shouldFlipHorizontal) {
                            selector = 'left';
                        }
                    }

                    // For mud-popover-bottom-center
                    else if (classList.contains('mud-popover-bottom-center')) {
                        // Space available in current vs opposite direction
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin;

                        if (popoverHeight > spaceAbove && spaceBelow > spaceAbove) {
                            selector = 'bottom';
                        }
                    }

                    // For mud-popover-bottom-right
                    else if (classList.contains('mud-popover-bottom-right')) {
                        // Space available in current direction
                        const spaceAbove = anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceLeft = anchorX - window.mudpopoverHelper.flipMargin;

                        // Space available in opposite direction
                        const spaceBelow = window.innerHeight - anchorY - window.mudpopoverHelper.flipMargin;
                        const spaceRight = window.innerWidth - anchorX - window.mudpopoverHelper.flipMargin;

                        // Check if popover exceeds available space AND if opposite side has more space
                        const shouldFlipVertical = popoverHeight > spaceAbove && spaceBelow > spaceAbove;
                        const shouldFlipHorizontal = popoverWidth > spaceLeft && spaceRight > spaceLeft;

                        if (shouldFlipVertical && shouldFlipHorizontal) {
                            selector = 'bottom-and-right';
                        }
                        else if (shouldFlipVertical) {
                            selector = 'bottom';
                        }
                        else if (shouldFlipHorizontal) {
                            selector = 'right';
                        }
                    }

                }

                // selector is set in above if statement if it needs to flip
                if (selector && selector != 'none') {
                    const newPosition = window.mudpopoverHelper.getPositionForFlippedPopver(classListArray, selector, boundingRect, selfRect);
                    left = newPosition.left;
                    top = newPosition.top;
                    offsetX = newPosition.offsetX;
                    offsetY = newPosition.offsetY;
                    popoverContentNode.setAttribute('data-mudpopover-flip', selector);
                }
                else {
                    popoverContentNode.removeAttribute('data-mudpopover-flip');
                }

                if (isFlipOnOpen) { // store flip direction on open so it's not recalculated
                    if (!popoverContentNode.mudPopoverFliped) {
                        popoverContentNode.mudPopoverFliped = selector || 'none';
                    }
                }

                // ensure the left is inside bounds
                if (left + offsetX < window.mudpopoverHelper.overflowPadding && // it's starting left of the screen
                    Math.abs(left + offsetX) < selfRect.width) { // it's not starting so far left the entire box would be hidden
                    left = window.mudpopoverHelper.overflowPadding;
                    // set offsetX to 0 to avoid double offset
                    offsetX = 0;
                }

                // ensure the top is inside bounds
                if (top + offsetY < window.mudpopoverHelper.overflowPadding && // it's starting above the screen
                    boundingRect.top >= 0 && // the popoverNode is still on screen
                    Math.abs(top + offsetY) < selfRect.height) { // it's not starting so far above the entire box would be hidden
                    top = window.mudpopoverHelper.overflowPadding;
                    // set offsetY to 0 to avoid double offset
                    offsetY = 0;
                }

                // will be covered by appbar so adjust zindex with appbar as parent
                if (top + offsetY < appBarOffset &&
                    appBarElements.length > 0) {
                    this.updatePopoverZIndex(popoverContentNode, appBarElements[0]);
                }

                const firstChild = popoverContentNode.firstElementChild;

                // adjust the popover position/maxheight if it or firstChild does not have a max-height set (even if set to 'none')
                // exceeds the bounds and doesn't have a max-height set by the user
                // maxHeight adjustments stop the minute popoverNode is no longer inside the window
                // Check if max-height is set on popover or firstChild
                const hasMaxHeight = popoverContentNode.style.maxHeight != '' || (firstChild && firstChild.style.maxHeight != '');

                if (!hasMaxHeight) {
                    // in case of a reflow check it should show from top properly
                    let shouldShowFromTop = false;
                    // calculate new max height if it exceeds bounds
                    let newMaxHeight = window.innerHeight - top - offsetY - window.mudpopoverHelper.overflowPadding; // downwards

                    // Check if this is a flipped popover showing upward
                    // Convert classList to an array and check if any class contains the substring
                    const isCentered = Array.from(classList).some(className => className.includes('mud-popover-anchor-center'));
                    const isFlippedUpward = !isCentered && ( // center anchors don't flip
                        popoverContentNode.getAttribute('data-mudpopover-flip') === 'top' ||
                        popoverContentNode.getAttribute('data-mudpopover-flip') === 'top-and-left' ||
                        popoverContentNode.getAttribute('data-mudpopover-flip') === 'top-and-right');

                    // moving upwards
                    if (top + offsetY < anchorY || top + offsetY == window.mudpopoverHelper.overflowPadding) {
                        shouldShowFromTop = true;
                        // adjust newMaxHeight if flipped upwards
                        if (isFlippedUpward) {
                            newMaxHeight = anchorY - window.mudpopoverHelper.overflowPadding - popoverNode.offsetHeight;
                        }
                        // adjust newMaxHeight if not flipped upwards
                        else {
                            newMaxHeight = anchorY - window.mudpopoverHelper.overflowPadding;
                        }
                    }

                    // if calculated height exceeds the new maxheight
                    if (popoverContentNode.offsetHeight > newMaxHeight) {
                        if (shouldShowFromTop) { // adjust top to show from top
                            // also adjust newMaxHeight 
                            top = window.mudpopoverHelper.overflowPadding;
                            offsetY = 0;
                        }
                        popoverContentNode.style.maxHeight = (newMaxHeight) + 'px';
                        popoverContentNode.mudHeight = "setmaxheight";
                    }
                }
            }

            if (isPositionFixed) {
                popoverContentNode.style['position'] = 'fixed';
            }
            else if (!classList.contains('mud-popover-fixed')) {
                offsetX += window.scrollX;
                offsetY += window.scrollY
            }

            popoverContentNode.style['left'] = (left + offsetX) + 'px';
            popoverContentNode.style['top'] = (top + offsetY) + 'px';

            // update z-index by sending the calling popover to update z-index,
            // and the parentnode of the calling popover (not content parent)
            this.updatePopoverZIndex(popoverContentNode, popoverNode.parentNode);

            if (!zIndexAuto) {
                popoverContentNode.style['z-index'] = Math.max(popoverNodeStyle.getPropertyValue('z-index'), popoverContentNode.style['z-index']);
                popoverContentNode.skipZIndex = true;
            }

            // adjust overlays as needed with new zindex
            window.mudpopoverHelper.popoverOverlayUpdates();
        }
        else {
            //console.log(`popoverNode: ${popoverNode} ${popoverNode ? popoverNode.parentNode : ""}`);
        }
    };