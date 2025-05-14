const observerMap = new Map();

export function getViewportCorners(element, popoverId, isLeft, scrollNodeSelector = null) {
    const scrollNode = document.querySelector(scrollNodeSelector) || element.parentNode.parentNode;
    const isFixed = scrollNodeSelector === null;

    const getAbsolutePosition = (el) => {
        let x = 0, y = 0;
        if (isFixed) {
            // For fixed positioning, use getBoundingClientRect for viewport coordinates
            const rect = el.getBoundingClientRect();
            x = rect.left + window.scrollX;
            y = rect.top + window.scrollY;
        } else {
            // For absolute positioning, use offsetLeft/offsetTop relative to offsetParent
            x = el.offsetLeft;
            y = el.offsetTop;
            // If offsetParent is not containerDiv, adjust to containerDiv's coordinates
            const containerDiv = el.closest('[style*="position: relative"]') || scrollNode;
            if (el.offsetParent !== containerDiv) {
                const containerRect = containerDiv.getBoundingClientRect();
                const elementRect = el.getBoundingClientRect();
                x = elementRect.left - containerRect.left;
                y = elementRect.top - containerRect.top;
            }
            // Adjust for scrollNode's scroll position to keep popover in place
            x += scrollNode.scrollLeft || 0;
            y += scrollNode.scrollTop || 0;
        }
        return { x, y };
    };

    const updatePosition = () => {
        const marginLeft = parseInt(window.getComputedStyle(element).marginLeft, 10);
        const { x, y } = getAbsolutePosition(element);

        const popover = document.getElementById(popoverId);
        if (popover) {
            const adjustedX = isLeft ? x - marginLeft : x + element.offsetWidth;
            popover.style.left = `${adjustedX}px`;
            popover.style.top = `${y}px`;
        }
    };

    // Clean up any existing observer for this popoverId
    disposePopoverResize(popoverId);

    // Ensure scrollNode or containerDiv is positioned for non-fixed case
    if (!isFixed) {
        const containerDiv = element.closest('[style*="position: relative"]') || scrollNode;
        if (window.getComputedStyle(containerDiv).position === 'static') {
            containerDiv.style.position = 'relative';
        }
    }

    // Initial position
    updatePosition();

    // Setup new observers
    const elementObserver = new ResizeObserver(updatePosition);
    elementObserver.observe(element);

    // Observe scrollNode for scroll and resize events
    const scrollObserver = new ResizeObserver(updatePosition);
    if (!isFixed) {        
        scrollObserver.observe(scrollNode);
        scrollNode.addEventListener('scroll', updatePosition, { passive: true });
    }

    // Store observers with reference to the actual element
    observerMap.set(popoverId, {
        elementObserver,
        scrollObserver: !isFixed ? scrollObserver : null,
        scrollListener: !isFixed ? updatePosition : null,
        scrollNode: !isFixed ? scrollNode : null,
    });

    return true;
}

export function disposePopoverResize(popoverId) {
    if (observerMap.has(popoverId)) {
        const { elementObserver, scrollObserver, scrollListener, scrollNode } = observerMap.get(popoverId);

        elementObserver.disconnect();
        if (scrollObserver) scrollObserver.disconnect();
        if (scrollListener && scrollNode) scrollNode.removeEventListener('scroll', scrollListener);

        observerMap.delete(popoverId);
    }
}