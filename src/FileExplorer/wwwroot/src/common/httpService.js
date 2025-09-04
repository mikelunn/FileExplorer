class HttpService {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
        if (!this.baseUrl) {
            throw new Error("Base URL is required");
        }
        this.baseUrl = baseUrl.replace(/\/+$/, "");
        this.loader = document.getElementById('loader');
        this.activeRequests = 0;
    }
    async request(path, { queryParams = {}, headers = {}, responseType, ...options } = {}) {
        try {
            this._showLoader();
            const cleanPath = path.replace(/^\/+/, ''); // remove leading slash
            const url = new URL(
                cleanPath ? `${this.baseUrl}/${cleanPath}` : this.baseUrl,
                window.location.origin
            );
            Object.entries(queryParams).forEach(([key, value]) => {
                if (value !== undefined && value !== null) {
                    url.searchParams.append(key, value);
                }
            });
            const isFormData = options.body instanceof FormData;

            const response = await fetch(url, {
                headers: {
                    ...(isFormData ? {} : { "Content-Type": "application/json" }),
                    ...headers,
                },
                ...options,
            });


            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(
                    `HTTP error ${response.status}: ${response.statusText}\n${errorText}`
                );
            }
            if (responseType === 'blob') {
                return await response.blob();
            }

            // Try to parse JSON, otherwise return text
            const contentType = response.headers.get("content-type");
            if (contentType && contentType.includes("application/json")) {
                return await response.json();
            }
            return await response.text();

        } finally {
            this._hideLoader();
        }
        
    }
    _showLoader() {
        this.activeRequests++;
        if (this.loader) this.loader.style.display = 'flex';
    }

    _hideLoader() {
        this.activeRequests = Math.max(0, this.activeRequests - 1);
        if (this.activeRequests === 0 && this.loader) this.loader.style.display = 'none';
    }
}
export default HttpService;