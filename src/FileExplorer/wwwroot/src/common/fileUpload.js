class FileUpload {
    constructor(containerId, { onUpload } = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) throw new Error(`Container ${containerId} not found.`);
        this.onUpload = onUpload;
        this._render();
        this._bindEvents();
    }

    _render() {
        this.container.innerHTML = `
            <label for="file-input" class="upload-btn">⬆ Upload</label>
            <input type="file" id="file-input" hidden>

        `;
        this.fileInput = this.container.querySelector("#file-input");
    }

    _bindEvents() {
        this.fileInput.addEventListener("change", () => {
            const file = this.fileInput.files[0];
            if (!file) return;

            if (typeof this.onUpload === "function") {
                this.onUpload(file);
                this.fileInput.value = "";
            }
        });
    }
}

export default FileUpload;
