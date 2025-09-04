class FileList {
    constructor(containerId, { onClick } = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) throw new Error(`Container ${containerId} not found.`);
        this.files = [];
        this.onClick = onClick;
        this._bindEvents();
    }

    setFiles(files) {
        this.files = files || [];
        this.render();
    }

    render() {
        this.container.innerHTML = this.files
            .map((item, idx) => {
                let info = '';
                if (item.isDirectory) {
                    const sizeMB = (item.size / (1024 * 1024)).toFixed(2);
                    info = ` (${item.fileCount} files, ${item.folderCount} folders, ${sizeMB} MB)`;
                } else {
                    const sizeKB = (item.size / 1024).toFixed(1);
                    info = ` (${sizeKB} KB)`;
                }
                return `
                <li class="${item.isDirectory ? 'folder' : 'file'}" data-index="${idx}">
                    <span class="file-name">${item.name}${info}</span>
                    <button data-action="copy">⧉</button>
                    <button data-action="move">⇨</button>
                    <button type="button" data-action="delete" class="delete-btn" data-index="${idx}" aria-label="Delete file">&times;</button>
                    ${!item.isDirectory ?

                        `   <button type="button" data-action="download" class="download-btn" aria-label="Download file">⬇</button>
                    ` : ''}
                </li>
            `
            }).join('');
    }

    _bindEvents() {
        this.container.addEventListener("click", async (e) => {
            e.preventDefault();
            e.stopPropagation();

            const li = e.target.closest("li[data-index]");
            if (!li) return;

            const index = parseInt(li.dataset.index, 10);
            const item = this.files[index];
            const action = e.target.dataset?.action;

            if (this.onClick) {
                try {
                    await this.onClick(item, action);
                } catch (err) {
                    console.error("FileList onClick error:", err);
                }
            }

            return false;
        });
    }
}

export default FileList;
