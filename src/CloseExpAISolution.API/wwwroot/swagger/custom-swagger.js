// Auto-fill JWT token after login
(function () {
    const originalFetch = window.fetch;

    window.fetch = function (...args) {
        return originalFetch.apply(this, args).then(async (response) => {
            // Clone response to read body without consuming it
            const clonedResponse = response.clone();

            try {
                const url = args[0];

                // Check if this is a login request
                if (url && (url.includes('/api/Auth/login') || url.includes('/api/auth/login'))) {
                    const data = await clonedResponse.json();

                    // Extract token from response (adjust path based on your response structure)
                    let token = null;

                    // Try different response structures
                    if (data.data?.accessToken) {
                        token = data.data.accessToken;
                    } else if (data.accessToken) {
                        token = data.accessToken;
                    } else if (data.data?.token) {
                        token = data.data.token;
                    } else if (data.token) {
                        token = data.token;
                    }

                    if (token) {
                        // Set token in Swagger UI
                        const authBtn = document.querySelector('.btn.authorize');
                        if (authBtn) {
                            authBtn.click();

                            setTimeout(() => {
                                const input = document.querySelector('.auth-container input[type="text"]');
                                if (input) {
                                    // Set the token value
                                    const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                                    nativeInputValueSetter.call(input, token);
                                    input.dispatchEvent(new Event('input', { bubbles: true }));

                                    // Click Authorize button
                                    setTimeout(() => {
                                        const authorizeBtn = document.querySelector('.auth-btn-wrapper .btn-done') ||
                                            document.querySelector('.auth-btn-wrapper button.authorize');
                                        if (authorizeBtn && authorizeBtn.textContent.includes('Authorize')) {
                                            authorizeBtn.click();

                                            // Close the modal
                                            setTimeout(() => {
                                                const closeBtn = document.querySelector('.auth-btn-wrapper .btn-done') ||
                                                    document.querySelector('button.btn-done');
                                                if (closeBtn) {
                                                    closeBtn.click();
                                                }
                                            }, 300);
                                        }
                                    }, 200);
                                }
                            }, 300);
                        }

                        console.log('✅ Token auto-filled successfully!');
                    }
                }
            } catch (e) {
                // Ignore JSON parse errors for non-JSON responses
            }

            return response;
        });
    };

    console.log('🔐 Swagger Auto-Auth Extension loaded');
})();
