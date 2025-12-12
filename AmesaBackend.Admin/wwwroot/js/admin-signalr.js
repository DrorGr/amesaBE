// SignalR client for Admin Panel real-time updates
(function () {
    let connection = null;

    function initializeSignalR() {
        const signalRUrl = '/hub'; // Relative to base href /admin/, resolves to /admin/hub
        
        // Check if SignalR is available
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded. Real-time updates will not work.');
            return;
        }

        connection = new SignalR.HubConnectionBuilder()
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
                // Join relevant groups
                connection.invoke('JoinGroup', 'houses').catch(err => console.error('Failed to join houses group:', err));
                connection.invoke('JoinGroup', 'users').catch(err => console.error('Failed to join users group:', err));
                connection.invoke('JoinGroup', 'draws').catch(err => console.error('Failed to join draws group:', err));
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
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

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeSignalR);
    } else {
        initializeSignalR();
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', () => {
        if (connection) {
            connection.stop();
        }
    });
})();
