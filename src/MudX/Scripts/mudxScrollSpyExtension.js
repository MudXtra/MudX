class MudXScrollSpy {
    constructor() {
        this.handlerRef = null
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        this.dotNetReference = null;
        this.containerSelector = null;
        this.sectionClassSelector = null;
    }

    spying(dotnetReference, containerSelector, sectionClassSelector) {
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        this.dotNetReference = dotnetReference;
        this.container = null;
        this.containerSelector = containerSelector;
        this.sectionClassSelector = sectionClassSelector;
        this.handlerRef = this.handleScroll.bind(this);

        window.addEventListener("scroll", this.handlerRef, true); // in case for zooming
        window.addEventListener("resize", this.handlerRef, true); // in case for resizing
    }

    unspy() {
        window.removeEventListener("scroll", this.handlerRef, true);
        window.removeEventListener("resize", this.handlerRef, true);
        if (this.pendingAnimation !== null) {
            cancelAnimationFrame(this.pendingAnimation);
        }
        this.handlerRef = null
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        this.dotNetReference = null;
        this.container = null;
        this.containerSelector = null;
        this.sectionClassSelector = null;
    }

    handleScroll() {
        if (this.pendingAnimation !== null) {
            cancelAnimationFrame(this.pendingAnimation);
        }

        this.pendingAnimation = requestAnimationFrame(() => {
            this.pendingAnimation = null;
            if (this.handlerRef === null) return;

            const container = document.querySelector(this.containerSelector);
            if (!container) return;

            const sections = container.querySelectorAll(this.sectionClassSelector);
            if (sections.length === 0) return;

            const containerTop = container.tagName === 'HTML' ? 0 : container.getBoundingClientRect().top;
            const containerHeight = container.clientHeight;
            const center = containerTop + containerHeight / 2;

            let minDifference = Number.MAX_SAFE_INTEGER;
            let foundAbove = false;
            let elementId = '';
            for (let i = 0; i < sections.length; i++) {
                const section = sections[i];
                const rect = section.getBoundingClientRect();
                const diff = Math.abs(rect.top - center);
                if (!foundAbove && rect.top < center) {
                    foundAbove = true;
                    minDifference = diff;
                    elementId = section.id;
                    continue;
                }
                if (foundAbove && rect.top >= center) {
                    continue;
                }
                if (diff < minDifference) {
                    minDifference = diff;
                    elementId = section.id;
                }
            }

            if (elementId !== this.lastKnownElement) {
                this.lastKnownElement = elementId;
                if (this.dotNetReference === null) return;
                history.replaceState(null, '', window.location.pathname + "#" + elementId);
                this.dotNetReference.invokeMethodAsync("ScrollToSection", elementId);
            }
        });
    }

    activateSection(sectionId) {
        const container = document.querySelector(this.containerSelector);
        if (!container) return;

        const section = container.querySelector(`$#${sectionId}`);
        if (section) {
            this.lastKnownElement = sectionId;
            history.replaceState(null, '', window.location.pathname + "#" + sectionId);
        }
    }

    scrollToSection(sectionId) {
        if (sectionId) {
            const container = document.querySelector(this.containerSelector);
            if (!container) return;

            let element = container.querySelector(`#${sectionId}`) || document.getElementById(sectionId);
            if (element) {
                element.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'start' });
            }
        }
        else {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    }
}