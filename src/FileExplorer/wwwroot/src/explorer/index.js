import FileList from './FileList.js';
import Breadcrumb from '../common/breadcrumb.js';
import UrlService from '../common/urlService.js';
import FileUpload from '../common/fileUpload.js';
import HttpService from '../common/httpService.js';
import FileSearch from '../common/fileSearch.js';

class ExplorerApp {
    constructor() {
        this.defaultHome = 'default';
        this.httpService = new HttpService('/api/v1/files');
        this.urlService = new UrlService({ onChange: this.onPathChange.bind(this) });
        this.fileList = new FileList('file-list', { onClick: this.handleFileEvent.bind(this) });
        this.breadcrumb = new Breadcrumb('breadcrumb', { onNavigate: ({ home, path }) => this.urlService.setParams({ home, path }) });
        this.fileUpload = new FileUpload('upload-container', { onUpload: this.handleUpload.bind(this) });
        this.fileSearch = new FileSearch('search-container', { onInput: this.searchFiles.bind(this) });

        this.init();
    }

    async init() {
        const { home, path } = this.getParams();
        await this.loadFiles(home, path);
        this.breadcrumb.setPath(home, path ? path.split('/').filter(Boolean) : []);
    }

    getParams() {
        const params = this.urlService.getParams();
        return { home: params.home || this.defaultHome, path: params.path || '' };
    }
    async updateFile({ home, path }, action) {
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
                    home,
                    source: path,
                    destination: destinationPath,
                    operation: action
                }
            });
            alert(`File updated successfully.`);
            await this.loadFiles(home, this.getParams().path);
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
    async downloadFile({ home, path }) {
        try {
            const params = { home, path };
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
    async deleteFile({ home, path }) {
        await this.httpService.request('', { method: 'DELETE', queryParams: { ...(home ? { home } : {}), ...(path ? { path } : {}) } });
        const { home: currentHome, path: currentPath } = this.getParams();
        await this.loadFiles(currentHome, currentPath);
        this.breadcrumb.setPath(currentHome, currentPath ? currentPath.split('/').filter(Boolean) : []);
    }

    async handleUpload(file) {
        const { home, path } = this.getParams();
        const filePath = path ? `${path}/${file.name}` : file.name;

        const formData = new FormData();
        formData.append('file', file, file.name);
        formData.append('home', home);
        formData.append('path', filePath);

        await this.httpService.request('', { method: "POST", body: formData, queryParams: { home, path: filePath } });

        await this.loadFiles(home, path);
        this.breadcrumb.setPath(home, path ? path.split('/').filter(Boolean) : []);
    }

    async navigate(event) {
        if (event.isDirectory) {
            const home = event.home || this.defaultHome;
            const path = event.path || '';
            this.breadcrumb.add(event.name);
            this.urlService.setParams({ home, path }); // update URL on directory navigation
        }
    }

    async onPathChange({ home, path }) {
        await this.loadFiles(home || this.defaultHome, path || '');
    }

    async loadFiles(home, path) {
        const queryParams = { ...(home ? { home } : {}), ...(path ? { path } : {}) };
        const files = await this.httpService.request('', { queryParams });
        this.fileList.setFiles(files);

        if (!this.breadcrumb.currentPath.length && files.length) {
            this.breadcrumb.add(files[0].home);
        }
    }
    async searchFiles(query) {
        if (!query) {
            const { home, path } = this.getParams();
            await this.loadFiles(home, path);
            return;
        }

        try {
            const results = await this.httpService.request('search', {
                queryParams: { home: this.defaultHome, query }
            });
            this.fileList.setFiles(results);
        } catch (err) {
            console.error(err);
            alert('Search failed.');
        }
    }
}

export default ExplorerApp;
