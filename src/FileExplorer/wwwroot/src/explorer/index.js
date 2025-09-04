import FileList from './fileList.js';
import Breadcrumb from '../common/breadcrumb.js';
import UrlService from '../common/urlService.js';
import FileUpload from '../common/fileUpload.js';
import HttpService from '../common/httpService.js';
import FileSearch from '../common/fileSearch.js';

class ExplorerApp {
    constructor() {
        this.httpService = new HttpService('/api/v1/files');
        this.urlService = new UrlService({ onChange: this.init.bind(this) });
        this.fileList = new FileList('file-list', { onClick: this.handleFileEvent.bind(this) });
        this.breadcrumb = new Breadcrumb('breadcrumb', {
            onNavigate: ({ path }) => this.urlService.setParams({ path })
        });
        this.fileUpload = new FileUpload('upload-container', { onUpload: this.handleUpload.bind(this) });
        this.fileSearch = new FileSearch('search-container', { onInput: this.searchFiles.bind(this) });

        this.init();
    }

    async init() {
        const { path } = this.getParams();
        const files = await this.loadFiles(path);
        this.fileList.setFiles(files);
        //filter truthy elements in array
        this.breadcrumb.setPath(path ? path.split('/').filter(Boolean) : []);
    }

    getParams() {
        const params = this.urlService.getParams();
        return { path: params.path || '' };
    }

    async handleFileEvent(file, action) {
        if (action === 'delete') {
            await this.deleteFile(file);
            return;
        }
        if (action === 'copy' || action === 'move') {
            await this.updateFile(file, action);
            return;
        }
        if (action === 'download') {
            await this.downloadFile(file);
            return;
        }
        else {
            await this.navigate(file);
        }
    }
    async updateFile({ path }, action) {
        const destination = prompt(`Enter destination folder path for file:`);
        if (!destination) return;
        const pathSegments = path.split('/');
        const filename = pathSegments[pathSegments.length - 1];

        //remove any trailing slashes from destination
        const destinationPath = destination
            ? `${destination.replace(/\/$/, '')}/${filename}`
            : filename;
        try {
            await this.httpService.request('', {
                method: "PUT",
                queryParams: {
                    source: path,
                    destination: destinationPath,
                    operation: action
                }
            });
            await this.init();
        } catch (err) {
            console.error(err);
            const msg = err?.message || 'Unknown error';
            alert(`${action} failed: ${msg}`);
        }
    }
    async deleteFile({ path }) {
        if (!path) return;
        await this.httpService.request('', { method: 'DELETE', queryParams: { path } });
        await this.init();
    }

    async downloadFile({ path }) {
        try {
            const blob = await this.httpService.request('download', {
                queryParams: { path },
                responseType: 'blob'
            });

            const filename = path.split('/').pop();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(url);
        } catch (err) {
            console.error('Download failed:', err);
            const msg = err?.message || 'Unknown error';
            alert(`Download failed: ${msg}`);
        }
    }

    async handleUpload(file) {
        const { path } = this.getParams();
        const filePath = path ? `${path}/${file.name}` : file.name;

        const formData = new FormData();
        formData.append('file', file, file.name);
        formData.append('path', filePath);

        await this.httpService.request('', { method: 'POST', body: formData });
        await this.init();
    }

    async navigate(file) {
        if (file.isDirectory) {
            this.urlService.setParams({ path: file.path });
        }
    }

    async loadFiles(path) {
        const queryParams = path ? { path } : {};
        return await this.httpService.request('', { queryParams });
    }
    async searchFiles(query) {
        if (!query) {
            this.init();
            return;
        }

        try {
            const results = await this.httpService.request('search', {
                queryParams: { query }
            });
            this.fileList.setFiles(results);
        } catch (err) {
            console.error(err);
            const msg = err?.message || 'Unknown error';
            alert(`Search failed: ${msg}`);
        }
    }
}

export default ExplorerApp;
