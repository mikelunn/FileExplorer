class FileSearch {
    constructor(containerId, { onInput } = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) throw new Error(`Container ${containerId} not found.`);
        this.onInput = onInput;

        this._render();
        this._bindEvents();
    }

    _render() {
        this.container.innerHTML = `
            <input type="text" id="file-search-input" placeholder="Search files/folders..." />
            <button type="button" id="file-search-btn">🔍</button>
        `;
        this.input = this.container.querySelector("#file-search-input");
        this.button = this.container.querySelector("#file-search-btn");
    }

    _bindEvents() {
        this.button.addEventListener("click", () => this._triggerSearch());
        this.input.addEventListener("keyup", (e) => {
            if (e.key === "Enter") this._triggerSearch();
        });
    }

    _triggerSearch() {
        const query = this.input.value.trim();
        if (typeof this.onInput === "function") {
            this.onInput(query);
        }
    }

    clear() {
        this.input.value = "";
    }
}

export default FileSearch;
