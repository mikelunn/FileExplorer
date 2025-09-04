import FileList from './FileList.js';
import Breadcrumb from '../common/breadcrumb.js';
import UrlService from '../common/urlService.js';
import FileUpload from '../common/fileUpload.js';
import HttpService from '../common/httpService.js';
import FileSearch from '../common/fileSearch.js';

class ExplorerApp {
    constructor() {
        this.httpService = new HttpService('/api/v1/files');
        this.urlService = new UrlService({ onChange: this.onPathChange.bind(this) });
        this.fileList = new FileList('file-list', { onClick: this.handleFileEvent.bind(this) });
        this.breadcrumb = new Breadcrumb('breadcrumb', { onNavigate: ({ home, path }) => this.urlService.setParams({ home, path }) });
        this.fileUpload = new FileUpload('upload-container', { onUpload: this.handleUpload.bind(this) });
        this.fileSearch = new FileSearch('search-container', { onInput: this.searchFiles.bind(this) });

        this.init();
    }

    async init() {
        const { path } = this.getParams();
        await this.loadFiles(path);
        this.breadcrumb.setPath(path ? path.split('/').filter(Boolean) : []);
    }

    getParams() {
        const params = this.urlService.getParams();
        return { path: params.path || '' };
    }
    async updateFile({path }, action) {
        const destination = prompt(`Enter destination folder path for file:`);
        if (!destination) return;
        const pathSegments = path.split('/');
        const filename = pathSegments[pathSegments.length - 1];

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
            await this.loadFiles(this.getParams()?.path);
        } catch (err) {
            console.error(err);
            alert(`${action} failed.`);
        }
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
    async downloadFile({ path }) {
        try {
            const params = { path };
            // Make async request using your HttpService
            const blob = await this.httpService.request('download', {
                queryParams: params,
                responseType: 'blob'
            });

            // Derive filename from path
            const filename = path.split('/').pop();

            // Create a temporary link to trigger download
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
            alert('Failed to download file.');
        }
    }
    async deleteFile({ path }) {
        await this.httpService.request('', { method: 'DELETE', queryParams: { ...(path ? { path } : {}) } });
        const { path: currentPath } = this.getParams();
        await this.loadFiles(currentPath);
        this.breadcrumb.setPath(currentPath ? currentPath.split('/').filter(Boolean) : []);
    }

    async handleUpload(file) {
        const { path } = this.getParams();
        const filePath = path ? `${path}/${file.name}` : file.name;

        const formData = new FormData();
        formData.append('file', file, file.name);
        formData.append('path', filePath);

        await this.httpService.request('', { method: "POST", body: formData });

        await this.loadFiles(path);
        this.breadcrumb.setPath(path ? path.split('/').filter(Boolean) : []);
    }

    async navigate(event) {
        if (event.isDirectory) {
            const path = event.path || '';
            this.breadcrumb.add(event.name);
            this.urlService.setParams({ path });
        }
    }

    async onPathChange({ path }) {
        await this.loadFiles(path || '');
    }


    async loadFiles(path) {
        const queryParams = { ...(path ? { path } : {}) };
        const files = await this.httpService.request('', { queryParams });
        this.fileList.setFiles(files);

        if (!this.breadcrumb.currentPath.length && path) {
            this.breadcrumb.setPath(path.split('/').filter(Boolean));
        }
    }
    async searchFiles(query) {
        if (!query) {
            const { path } = this.getParams();
            await this.loadFiles(path);
            return;
        }

        try {
            const results = await this.httpService.request('search', {
                queryParams: { query }
            });
            this.fileList.setFiles(results);
        } catch (err) {
            console.error(err);
            alert('Search failed.');
        }
    }
}

export default ExplorerApp;
