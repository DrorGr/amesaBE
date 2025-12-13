// SignalR client for Admin Panel real-time updates
(function () {
    let connection = null;
    let retryCount = 0;
    let isInitializing = false;
    let isInitialized = false;
    const maxRetries = 10; // Maximum 10 retries (1 second total)

    function initializeSignalR() {
        // Prevent multiple simultaneous initializations
        if (isInitializing || isInitialized) {
            return;
        }
        
        // CRITICAL FIX: Use absolute path /admin/hub instead of relative /hub
        // SignalR client doesn't always respect <base> tag, so we must be explicit
        const signalRUrl = '/admin/hub';
        
        // Check if signalR is fully available (wait for script to load and initialize)
        // signalR must have HubConnectionBuilder available to be usable
        // Note: window.SignalRLoaded flag may be set before signalR object is actually available,
        // so we check the actual signalR object, not just the flag
        // CRITICAL FIX: Microsoft SignalR exposes 'signalR' (camelCase), not 'SignalR' (PascalCase)
        var signalRAvailable = typeof signalR !== 'undefined' && 
                                signalR !== null &&
                                typeof signalR.HubConnectionBuilder !== 'undefined';
        
        // If SignalR is not fully available, retry
        if (!signalRAvailable) {
            retryCount++;
            if (retryCount <= maxRetries) {
                // Retry after a delay - use exponential backoff
                var delay = Math.min(200 * Math.pow(1.2, retryCount), 1000);
                setTimeout(initializeSignalR, delay);
                return;
            } else {
                // Max retries reached - SignalR library failed to load
                var signalRExists = typeof signalR !== 'undefined';
                var hasHubBuilder = signalRExists && typeof signalR.HubConnectionBuilder !== 'undefined';
                var flagSet = window.SignalRLoaded === true;
                console.warn('SignalR library failed to load after ' + maxRetries + ' attempts. SignalR available: ' + signalRExists + ', HubConnectionBuilder: ' + hasHubBuilder + ', Flag: ' + flagSet + '. Real-time updates will not be available.');
                isInitializing = false;
                return;
            }
        }
        
        // SignalR is available, proceed with initialization
        // Set the flag if not already set (backup)
        window.SignalRLoaded = true;
        
        // SignalR is available, mark as initializing
        isInitializing = true;
        retryCount = 0;

        // CRITICAL FIX: Use 'signalR' (camelCase) not 'SignalR' (PascalCase)
        connection = new signalR.HubConnectionBuilder()
            .withUrl(signalRUrl)
            .withAutomaticReconnect()
            .build();

        // Connection event handlers
        connection.onclose(() => {
            console.log('SignalR connection closed');
        });

        connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
        });

        connection.onreconnected(() => {
            console.log('SignalR reconnected');
        });

        // House update handlers
        connection.on('HouseCreated', (data) => {
            console.log('House created:', data);
            showNotification(`New house created: ${data.Title}`, 'success');
            // Refresh house list if on houses page (dynamic path check)
            const currentPath = window.location.pathname;
            if (currentPath.includes('/houses') || currentPath.endsWith('/admin/houses')) {
                setTimeout(() => window.location.reload(), 2000);
            }
        });

        connection.on('HouseUpdated', (data) => {
            console.log('House updated:', data);
            showNotification(`House updated: ${data.Title}`, 'info');
            // Refresh house list if on houses page (dynamic path check)
            const currentPath = window.location.pathname;
            if (currentPath.includes('/houses') || currentPath.endsWith('/admin/houses')) {
                setTimeout(() => window.location.reload(), 2000);
            }
        });

        connection.on('HouseDeleted', (data) => {
            console.log('House deleted:', data);
            showNotification('House deleted', 'warning');
            // Refresh house list if on houses page (dynamic path check)
            const currentPath = window.location.pathname;
            if (currentPath.includes('/houses') || currentPath.endsWith('/admin/houses')) {
                setTimeout(() => window.location.reload(), 1000);
            }
        });

        // User update handlers
        connection.on('UserUpdated', (data) => {
            console.log('User updated:', data);
            showNotification(`User updated: ${data.Email}`, 'info');
            // Refresh user list if on users page (dynamic path check)
            const currentPath = window.location.pathname;
            if (currentPath.includes('/users') || currentPath.endsWith('/admin/users')) {
                setTimeout(() => window.location.reload(), 2000);
            }
        });

        // Draw handlers
        connection.on('DrawConducted', (data) => {
            console.log('Draw conducted:', data);
            showNotification('Draw conducted', 'success');
            // Refresh draws page if on draws page (dynamic path check)
            const currentPath = window.location.pathname;
            if (currentPath.includes('/draws') || currentPath.endsWith('/admin/draws')) {
                setTimeout(() => window.location.reload(), 2000);
            }
        });

        // Start connection
        connection.start()
            .then(() => {
                console.log('SignalR connected');
                isInitialized = true;
                isInitializing = false;
                // Join relevant groups
                connection.invoke('JoinGroup', 'houses').catch(err => console.error('Failed to join houses group:', err));
                connection.invoke('JoinGroup', 'users').catch(err => console.error('Failed to join users group:', err));
                connection.invoke('JoinGroup', 'draws').catch(err => console.error('Failed to join draws group:', err));
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                isInitializing = false;
            });
    }

    function showNotification(message, type) {
        // Simple notification - can be enhanced with a toast library
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    // Initialize when window is fully loaded (ensures all scripts are loaded)
    // Use a single initialization point to prevent multiple calls
    function startInitialization() {
        if (!isInitialized && !isInitializing) {
            // Check if signalR script is already loaded
            // CRITICAL FIX: Use 'signalR' (camelCase) not 'SignalR' (PascalCase)
            if (typeof signalR !== 'undefined') {
                initializeSignalR();
            } else {
                // Wait a bit for signalR script to load from CDN
                // Increased delay to 1000ms to account for slower CDN loads
                setTimeout(initializeSignalR, 1000);
            }
        }
    }
    
    if (document.readyState === 'complete') {
        // Page already loaded
        startInitialization();
    } else {
        // Wait for window load event to ensure all scripts are loaded
        window.addEventListener('load', startInitialization, { once: true });
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', () => {
        if (connection) {
            connection.stop();
        }
    });
})();

