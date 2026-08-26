import axios from "axios";

// The JWT is intentionally never available to JavaScript. The API issues it
// as a Secure, HttpOnly cookie and this value protects authenticated writes.
let csrfToken = null;

const clearLegacyBrowserTokens = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("role");
};

// Remove credentials left behind by the pre-cookie authentication design.
clearLegacyBrowserTokens();

const API = axios.create({
    baseURL: import.meta.env.VITE_API_URL,
    withCredentials: true,
});

export const clearAuthState = () => {
    csrfToken = null;
};

const rememberSession = session => {
    csrfToken = session?.csrfToken || null;
    return session;
};

export const getApiUrl = (path = "") =>
    `${API.defaults.baseURL.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`;

API.interceptors.request.use((config) => {
    const method = String(config.method || "get").toUpperCase();
    if (!["GET", "HEAD", "OPTIONS"].includes(method) && csrfToken) {
        config.headers["X-CSRF-Token"] = csrfToken;
    }
    return config;
});

API.interceptors.response.use(
    response => response,
    error => {
        if (error.response?.status === 401) {
            clearAuthState();
            if (["/admin", "/dashboard", "/onboard", "/hr", "/hr/change-password"].includes(window.location.pathname)) {
                window.location.assign("/kiosk");
            }
        }
        return Promise.reject(error);
    }
);

export const login = async loginData =>
    rememberSession((await API.post("/Auth/login", loginData)).data);

export const getSession = async () =>
    rememberSession((await API.get("/Auth/session")).data);

export const logout = async () => {
    try {
        await API.post("/Auth/logout");
    } finally {
        clearAuthState();
    }
};

export const getHrAccountStatus = async () => (await API.get("/Auth/hr-account")).data;
export const createHrAccount = async accountData => (await API.post("/Auth/hr-account", accountData)).data;
export const changeHrPassword = async passwordData =>
    rememberSession((await API.post("/Auth/change-password", passwordData)).data);

export default API;
