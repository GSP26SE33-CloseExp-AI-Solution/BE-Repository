// ============================================================
// Swagger Auto-Auth Extension  [v2]
// Intercepts login API calls and auto-fills Bearer token
// Supports: POST /api/auth/login, POST /api/auth/google-login
// ============================================================
(function () {
    const AUTH_ENDPOINTS = [
        '/api/auth/login',
        '/api/auth/google-login',
    ];

    // Recursively search for accessToken / token at any nesting depth
    function extractToken(obj) {
        if (!obj || typeof obj !== 'object') return null;
        if (typeof obj.accessToken === 'string') return obj.accessToken;
        if (typeof obj.token === 'string') return obj.token;
        for (const val of Object.values(obj)) {
            const found = extractToken(val);
            if (found) return found;
        }
        return null;
    }

    // Inject token into Swagger UI's Authorize modal
    function injectTokenIntoSwagger(token) {
        const authBtn = document.querySelector('.btn.authorize');
        if (!authBtn) return;

        authBtn.click();

        setTimeout(() => {
            const input = document.querySelector('.auth-container input[type="text"]');
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
                const url = (args[0] ?? '').toString().toLowerCase();
                const isAuthCall = AUTH_ENDPOINTS.some(ep => url.includes(ep));

                if (isAuthCall) {
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

    console.log('🔐 [SwaggerAuth] Auto-Auth Extension loaded | endpoints:', AUTH_ENDPOINTS);
})();
