// mudxOutline
const observerMap = new Map();

export function getViewportCorners(element, popoverId, isLeft, scrollNodeSelector = null) {
    if (!element) return false;

    const popover = document.getElementById(popoverId);
    if (!popover) return false;
    const scrollNode = document.querySelector(scrollNodeSelector) || element.parentNode.parentNode;
    const isFixed = scrollNodeSelector === null;

    const updatePosition = () => {
        try {
            if (!element || !popover) return false;

            const elRect = element.getBoundingClientRect();
            const scrollRect = scrollNode.getBoundingClientRect();
            const rect = isFixed ? elRect : scrollRect;
            const marginLeft = parseInt(window.getComputedStyle(element).marginLeft, 10) || 0;
            const adjustedX = isLeft ? rect.left : rect.left + rect.width;
            const adjustedY = isFixed ? rect.top : rect.top;

            popover.style.position = isFixed ? 'fixed' : 'absolute';
            popover.style.left = `${adjustedX}px`;
            popover.style.top = `${adjustedY}px`;

            return true;
        } catch (e) {
            console.debug('Position update failed', e);
        }
        return false;
    };


    // Clean up any existing observer for this popoverId
    disposePopoverResize(popoverId);

    // Initial position
    updatePosition();

    // Setup new observers
    const elementObserver = new ResizeObserver(updatePosition);
    elementObserver.observe(element);

    // For absolute positioning, we need to observe container scroll
    const scrollObserver = new ResizeObserver(updatePosition);
    if (!isFixed) {        
        scrollObserver.observe(scrollNode);
        window.addEventListener('scroll', updatePosition, { passive: true });
    }

    // Store observers with reference to the actual element
    observerMap.set(popoverId, {
        elementObserver: elementObserver,
        scrollObserver: !isFixed ? scrollObserver : null,
        scrollListener: !isFixed ? updatePosition : null,
        element: element,
        parent: !isFixed ? element.parentNode : null
    });

    return true;
}

export function disposePopoverResize(popoverId) {
    if (observerMap.has(popoverId)) {
        const { elementObserver, scrollObserver, scrollListener, parent } = observerMap.get(popoverId);

        elementObserver.disconnect();
        if (scrollObserver) scrollObserver.disconnect();
        if (scrollListener && parent) parent.removeEventListener('scroll', scrollListener);

        observerMap.delete(popoverId);
    }
}