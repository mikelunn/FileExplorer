class UrlService {
    constructor({ onChange } = {}) {
        this.onChange = onChange;
        this._onPopState = this._onPopState.bind(this);
        window.addEventListener("popstate", this._onPopState);
    }

    getParams() {
        return Object.fromEntries(new URLSearchParams(window.location.search).entries());
    }

    setParams(updates = {}, { replace = false } = {}) {
        const params = new URLSearchParams(window.location.search);

        Object.entries(updates).forEach(([key, value]) => {
            if (value != null && value !== "") params.set(key, value);
            else params.delete(key);
        });

        const query = params.toString();
        const newUrl = query ? `${window.location.pathname}?${query}` : window.location.pathname;

        replace ? history.replaceState({}, "", newUrl) : history.pushState({}, "", newUrl);

        this.onChange?.(this.getParams());
    }

    _onPopState() {
        this.onChange?.(this.getParams());
    }

    init() {
        this.onChange?.(this.getParams());
    }

    destroy() {
        window.removeEventListener("popstate", this._onPopState);
    }
}

export default UrlService;
