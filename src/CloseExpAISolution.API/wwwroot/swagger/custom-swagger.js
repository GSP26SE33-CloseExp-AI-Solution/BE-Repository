// ============================================================
// Swagger Auto-Auth Extension  [v2]
// Intercepts login API calls and auto-fills Bearer token
// ============================================================
(function () {
    const AUTH_ENDPOINTS = [
        '/api/auth/login',
        '/api/auth/google-login',
    ];

    function normalizeRequestUrl(requestInfo) {
        if (!requestInfo) return '';
        if (typeof requestInfo === 'string') return requestInfo.toLowerCase();
        if (typeof requestInfo.url === 'string') return requestInfo.url.toLowerCase();
        return requestInfo.toString().toLowerCase();
    }

    function isAuthUrl(url) {
        return AUTH_ENDPOINTS.some(ep => url.includes(ep));
    }

    // Recursively search for accessToken / token at any nesting depth
    function extractToken(obj) {
        if (!obj) return null;

        if (typeof obj === 'string') {
            return obj.length > 20 ? obj : null;
        }

        if (typeof obj !== 'object') return null;

        for (const [key, val] of Object.entries(obj)) {
            const normalizedKey = key.toLowerCase();

            if (
                typeof val === 'string' &&
                (normalizedKey === 'accesstoken' ||
                    normalizedKey === 'token' ||
                    normalizedKey === 'jwt' ||
                    normalizedKey === 'jwt_token')
            ) {
                return val;
            }

            const found = extractToken(val);
            if (found) return found;
        }

        return null;
    }

    function applyTokenToSwagger(token) {
        try {
            if (window.ui) {
                // For OpenAPI 3, type: 'http', scheme: 'bearer'
                if (window.ui.authActions && typeof window.ui.authActions.authorize === 'function') {
                    window.ui.authActions.authorize({
                        Bearer: {
                            name: 'Bearer',
                            schema: {
                                type: 'http',
                                in: 'header',
                                name: 'Authorization',
                                description: ''
                            },
                            value: token
                        }
                    });
                    return true;
                } else if (typeof window.ui.preauthorizeApiKey === 'function') {
                    window.ui.preauthorizeApiKey('Bearer', token);
                    return true;
                }
            }
        } catch (_) {
            // Fallback to DOM injection below
        }
        return false;
    }

    // Inject token into Swagger UI's Authorize modal
    function injectTokenIntoSwagger(token) {
        if (applyTokenToSwagger(token)) {
            console.log('[SwaggerAuth] Token preauthorized via Swagger API');
            return;
        }

        const authBtn = document.querySelector('.btn.authorize');
        if (!authBtn) return;

        authBtn.click();

        setTimeout(() => {
            const input = document.querySelector('.auth-container input');
            if (!input) return;

            // React-compatible value setter
            const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
            setter.call(input, token);
            input.dispatchEvent(new Event('input', { bubbles: true }));

            setTimeout(() => {
                const authorizeBtn = document.querySelector('.auth-btn-wrapper button.authorize');
                if (!authorizeBtn) return;
                authorizeBtn.click();

                setTimeout(() => {
                    const closeBtn = document.querySelector('.auth-btn-wrapper .btn-done')
                        ?? document.querySelector('button.btn-done');
                    closeBtn?.click();
                }, 300);
            }, 200);
        }, 300);
    }

    // Intercept all fetch calls
    const originalFetch = window.fetch;
    window.fetch = function (...args) {
        return originalFetch.apply(this, args).then(async (response) => {
            try {
                const url = normalizeRequestUrl(args[0]);
                const isAuthCall = isAuthUrl(url);

                if (isAuthCall && response.ok) {
                    const data = await response.clone().json();
                    console.debug('[SwaggerAuth] Intercepted response:', data);
                    const token = extractToken(data);

                    if (token) {
                        injectTokenIntoSwagger(token);
                        console.log('✅ [SwaggerAuth] Token auto-filled from:', url);
                    } else {
                        console.warn('⚠️ [SwaggerAuth] No token found in response. Full data logged above.');
                    }
                }
            } catch (_) {
                // Non-JSON response or token injection error — safe to ignore
            }

            return response;
        });
    };

    // Intercept XHR as fallback for environments not using fetch directly
    const originalXhrOpen = XMLHttpRequest.prototype.open;
    const originalXhrSend = XMLHttpRequest.prototype.send;

    XMLHttpRequest.prototype.open = function (method, url, ...rest) {
        this.__swaggerAuthUrl = (url || '').toString().toLowerCase();
        return originalXhrOpen.call(this, method, url, ...rest);
    };

    XMLHttpRequest.prototype.send = function (...args) {
        this.addEventListener('load', function () {
            try {
                const url = this.__swaggerAuthUrl || '';
                if (!isAuthUrl(url)) return;
                if (this.status < 200 || this.status >= 300) return;

                const data = JSON.parse(this.responseText || '{}');
                const token = extractToken(data);
                if (token) {
                    injectTokenIntoSwagger(token);
                    console.log('✅ [SwaggerAuth] Token auto-filled from XHR:', url);
                } else {
                    console.warn('⚠️ [SwaggerAuth] No token found in XHR response.');
                }
            } catch (_) {
                // Ignore malformed/non-json responses
            }
        });

        return originalXhrSend.apply(this, args);
    };

    console.log('🔐 [SwaggerAuth] Auto-Auth Extension loaded | endpoints:', AUTH_ENDPOINTS);
})();
