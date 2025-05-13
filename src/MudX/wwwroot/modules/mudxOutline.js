// mudxOutline
const observerMap = new Map();

export function getViewportCorners(element, popoverId, isLeft) {
    if (!element) return false;

    const getAbsolutePosition = (el) => {
        let x = 0, y = 0;
        let current = el;
        while (current) {
            x += current.offsetLeft - current.scrollLeft;
            y += current.offsetTop - current.scrollTop;
            current = current.offsetParent;
        }
        return { x, y };
    };

    const updatePosition = () => {
        const popoverNode = document.getElementById(`$popover-{popoverId}`);
        window.mudpopoverHelper.placePopover(popoverNode);
        //try {
        //    if (!element) return false;

        //    //const marginLeft = parseInt(window.getComputedStyle(element).marginLeft, 10) || 0;
        //    const { x, y } = getAbsolutePosition(element);

        //    const popover = document.getElementById(popoverId);
        //    if (popover) { // - marginLeft
        //        const adjustedX = isLeft ? x : x + element.offsetWidth;
        //        popover.style.left = `${adjustedX}px`;
        //        popover.style.top = `${y}px`;
        //        return true;
        //    }
        //} catch (e) {
        //    console.debug('Position update failed (likely during disposal)', e);
        //}
        return false;
    };

    // Clean up any existing observer for this popoverId
    disposePopoverResize(popoverId);

    // Initial position
    updatePosition();

    // Setup new observer
    const observer = new ResizeObserver(updatePosition);
    observer.observe(element.parentNode);

    // Store observer with reference to the actual element
    observerMap.set(popoverId, {
        elementObserver: observer,
        element: element
    });

    return true;
}

export function disposePopoverResize(popoverId) {
    const observers = observerMap.get(popoverId);
    if (observers) {
        try {
            observers.elementObserver?.disconnect();
        } catch (e) {
            console.debug('Cleanup error', e);
        }
        observerMap.delete(popoverId);
    }
}