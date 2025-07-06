// MudXScrollSpy module
const scrollSpyInstances = {}; 

export class MudXScrollSpy {
    constructor() {
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        this.containerSelector = null;
        this.sectionClassSelector = null;
        this.dotNetReference = null;
        this.container = null;
        this.parentClassSelector = 'mudx-toc-document';
        this.handleRef = this.handleScroll.bind(this);
    }

    spying(containerSelector, sectionClassSelector, dotNetReference) {
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        if (!containerSelector || containerSelector == "#")
            containerSelector = "html"
        this.containerSelector = containerSelector;
        this.sectionClassSelector = sectionClassSelector;
        this.dotNetReference = dotNetReference;
        this.container = document.querySelector(this.containerSelector);
        if (!this.container) return;

        if (this.containerSelector !== 'html') {
            this.container.addEventListener('scroll', this.handleRef);
            this.container.addEventListener('resize', this.handleRef);
        } else {
            window.addEventListener('scroll', this.handleRef);
            window.addEventListener('resize', this.handleRef);
        }
    }

    unspy() {
        if (this.pendingAnimation !== null) {
            cancelAnimationFrame(this.pendingAnimation);
        }
        if (this.containerSelector !== 'html') {
            this.container?.removeEventListener('scroll', this.handleRef);
            this.container?.removeEventListener('resize', this.handleRef);
        } else {
            window.removeEventListener('scroll', this.handleRef);
            window.removeEventListener('resize', this.handleRef);
        }
        this.lastKnownElement = null;
        this.pendingAnimation = null;
        this.containerSelector = null;
        this.sectionClassSelector = null;
        this.dotNetReference = null;
        this.container = null;
    }

    handleScroll() {
        if (this.pendingAnimation !== null) {
            cancelAnimationFrame(this.pendingAnimation);
        }        

        this.pendingAnimation = requestAnimationFrame(() => {
            this.pendingAnimation = null;
            const container = this.container;
            if (!container) return;

            // Find the first mudx-toc-document (primary parent)
            const tocDoc = container.querySelector('.' + this.parentClassSelector);
            if (!tocDoc) return;

            // Find all mudx-toc-document elements
            const allTocDocuments = Array.from(container.querySelectorAll('.' + this.parentClassSelector));
            if (allTocDocuments.length === 0) return;

            // Remove our primary parent from the array (not with delete - that's for objects/maps)
            const otherTocDocs = allTocDocuments.filter(doc => doc !== tocDoc);

            // Collect all sections from other toc documents into a Set for quick lookup
            const sectionsInOtherDocs = new Set();
            otherTocDocs.forEach(doc => {
                const nestedSections = doc.querySelectorAll('.' + this.sectionClassSelector);
                nestedSections.forEach(section => sectionsInOtherDocs.add(section));
            });

            // Get all sections in our primary tocDoc
            const allSectionsInPrimary = Array.from(tocDoc.querySelectorAll('.' + this.sectionClassSelector));

            // Filter to only include sections that aren't in other toc documents
            const sections = allSectionsInPrimary.filter(section => !sectionsInOtherDocs.has(section));

            if (sections.length === 0) return;

            const containerTop = container.tagName === 'HTML' ? 0 : container.getBoundingClientRect().top;
            const containerHeight = container.clientHeight;
            const center = containerTop + containerHeight / 2;

            let minDifference = Number.MAX_SAFE_INTEGER;
            let foundAbove = false;
            let elementId = '';
            for (const section of sections) {
                const rect = section.getBoundingClientRect();
                const diff = Math.abs(rect.top - center);
                if (!foundAbove && rect.top < center) {
                    foundAbove = true;
                    minDifference = diff;
                    elementId = section.id;
                    continue;
                }
                if (foundAbove && rect.top >= center) continue;
                if (diff < minDifference) {
                    minDifference = diff;
                    elementId = section.id;
                }
            }

            if (elementId !== this.lastKnownElement) {
                this.lastKnownElement = elementId;
                if (this.dotNetReference) {
                    history.replaceState(null, '', `${window.location.pathname}#${elementId}`);
                    this.dotNetReference.invokeMethodAsync('SectionChangeOccured', elementId);
                }
            }
        });
    }

    activateSection(sectionId) {
        if (!this.container) return;
        const section = this.container.querySelector(`#${sectionId}`);
        if (section) {
            this.lastKnownElement = sectionId;
            history.replaceState(null, '', `${window.location.pathname}#${sectionId}`);
        }
    }

    scrollToSection(sectionId) {
        if (sectionId) {
            if (!this.container) return;
            const element = this.container.querySelector(`#${sectionId}`) || document.getElementById(sectionId);
            if (element) {
                element.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'start' });
            }
        } else {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    }
}

export function createScrollSpy(id) {
    const spy = new MudXScrollSpy();
    scrollSpyInstances[id] = spy;
    return spy;
}

export function getScrollSpy(id) {
    return scrollSpyInstances[id];
}

export function disposeScrollSpy(id) {
    const spy = scrollSpyInstances[id];
    if (spy) {
        spy.unspy();
        delete scrollSpyInstances[id];
    }
}