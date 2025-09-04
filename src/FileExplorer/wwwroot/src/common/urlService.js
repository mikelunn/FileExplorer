class UrlService {
    constructor({ onChange } = {}) {
        this.onChange = onChange;
        this._onPopState = this._onPopState.bind(this);
        window.addEventListener("popstate", this._onPopState);
    }

    getParams() {
        const params = new URLSearchParams(window.location.search);
        return Object.fromEntries(params.entries());
    }

    setParams(updates, { replace = false } = {}) {
        const params = new URLSearchParams(window.location.search);

        // Apply updates
        Object.entries(updates).forEach(([key, value]) => {
            if (value != null && value !== "") {
                params.set(key, value);
            } else {
                params.delete(key);
            }
        });

        // Build new URL safely (avoid "/?")
        const query = params.toString();
        const newUrl = query
            ? `${window.location.pathname}?${query}`
            : window.location.pathname;

        // Push or replace state
        if (replace) {
            history.replaceState({}, "", newUrl);
        } else {
            history.pushState({}, "", newUrl);
        }

        // Trigger callback
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