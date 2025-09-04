class Breadcrumb {
    constructor(containerId, { onNavigate } = {}) {
        this.container = document.getElementById(containerId);
        this.currentPath = [];
        this.onNavigate = onNavigate;
        this._bindEvents();
    }

    setPath(pathArray = []) {
        this.currentPath = ['Home', ...pathArray];
        this.render();
    }

    add(name) {
        this.currentPath.push(name);
        this.render();
    }

    render() {
        this.container.innerHTML = this.currentPath
            .map((folder, index) => `<span data-index="${index}">${folder}</span>`)
            .join(" ");
    }

    _bindEvents() {
        this.container.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();

            const span = e.target.closest("span[data-index]");
            if (!span) return;

            const index = parseInt(span.dataset.index, 10);
            this.currentPath = this.currentPath.slice(0, index + 1);
            this.render();

            this.onNavigate?.({
                path: index === 0
                    ? '' // "Home" = empty path
                    : this.currentPath.slice(1).join('/')
            });
        });
    }
}

export default Breadcrumb;
