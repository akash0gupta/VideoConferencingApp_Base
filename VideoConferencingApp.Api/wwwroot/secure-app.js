// Configuration
const API_URL = 'https://localhost:7157';
const HUB_URL = `${API_URL}/hubs/call`;

const ICE_SERVERS = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' },
        // In production, add TURN servers with authentication
        // {
        //     urls: 'turn:your-turn-server.com:3478',
        //     username: 'username',
        //     credential: 'password'
        // }
    ]
};

// Global state
let connection = null;
let peerConnection = null;
let localStream = null;
let accessToken = null;
let refreshToken = null;
let currentUserId = null;
let currentUsername = null;
let remoteUserId = null;

// Initialize on page load
window.addEventListener('load', async () => {
    await showLoginForm();
});

// Login functionality
async function showLoginForm() {
    const username = prompt('Enter your username:');
    const password = prompt('Enter your password:');

    if (!username || !password) {
        alert('Username and password are required');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ UsernameOrEmail: username, Password: password, RememberMe: false, TwoFactorCode: "", UserAgent: "", IpAddress:"" })
        });

        if (!response.ok) {
            throw new Error('Login failed');
        }

        const data = await response.json();

        // Store tokens securely
        accessToken = data.accessToken;
        refreshToken = data.refreshToken;
        currentUserId = data.userId;
        currentUsername = data.username;

        // Store in sessionStorage (not localStorage for better security)
        sessionStorage.setItem('accessToken', accessToken);
        sessionStorage.setItem('refreshToken', refreshToken);
        sessionStorage.setItem('userId', currentUserId);
        sessionStorage.setItem('username', currentUsername);

        console.log('✅ Login successful');

        // Update UI
        document.getElementById('currentUser').textContent = `${currentUsername} (${currentUserId})`;

        // Start local video
        await startLocalVideo();

        // Connect to SignalR with authentication
        await connectToSignalR();

    } catch (error) {
        console.error('❌ Login error:', error);
        alert('Login failed. Please try again.');
    }
}

// Connect to SignalR hub with JWT
async function connectToSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL, {
            accessTokenFactory: () => accessToken,
            transport: signalR.HttpTransportType.WebSockets // Force WebSockets for security
        })
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                // Exponential backoff
                return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Handle reconnection
    connection.onreconnecting(error => {
        console.log('⚠️ Reconnecting...', error);
    });

    connection.onreconnected(connectionId => {
        console.log('✅ Reconnected with connection ID:', connectionId);
        loadConnectedUsers();
    });

    connection.onclose(async error => {
        console.log('❌ Connection closed', error);

        // Try to refresh token and reconnect
        if (await refreshAccessToken()) {
            setTimeout(() => connectToSignalR(), 5000);
        } else {
            alert('Session expired. Please login again.');
            location.reload();
        }
    });

    // Register client methods
    registerSignalRHandlers();

    try {
        await connection.start();
        console.log('✅ Connected to SignalR with authentication');

        await loadConnectedUsers();

    } catch (error) {
        console.error('❌ SignalR connection error:', error);

        // Try to refresh token
        if (await refreshAccessToken()) {
            setTimeout(() => connectToSignalR(), 2000);
        } else {
            alert('Authentication failed. Please login again.');
            location.reload();
        }
    }
}

// Refresh access token
async function refreshAccessToken() {
    try {
        const response = await fetch(`${API_URL}/api/auth/refresh`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ refreshToken })
        });

        if (!response.ok) {
            return false;
        }

        const data = await response.json();

        accessToken = data.accessToken;
        refreshToken = data.refreshToken;

        sessionStorage.setItem('accessToken', accessToken);
        sessionStorage.setItem('refreshToken', refreshToken);

        console.log('✅ Token refreshed');
        return true;

    } catch (error) {
        console.error('❌ Token refresh failed:', error);
        return false;
    }
}

// Logout
async function logout() {
    try {
        await fetch(`${API_URL}/api/auth/logout`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': 'application/json'
            }
        });

        // Clear tokens
        sessionStorage.clear();

        // Close connection
        if (connection) {
            await connection.stop();
        }

        // Stop media streams
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
        }

        // Reload page
        location.reload();

    } catch (error) {
        console.error('❌ Logout error:', error);
    }
}

// Start local video stream
async function startLocalVideo() {
    try {
        localStream = await navigator.mediaDevices.getUserMedia({
            video: {
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: 'user'
            },
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            }
        });

        document.getElementById('localVideo').srcObject = localStream;
        console.log('✅ Local video started');
    } catch (error) {
        console.error('❌ Error accessing media devices:', error);
        alert('Could not access camera/microphone. Please grant permissions.');
    }
}

// ... (Rest of the SignalR handlers and WebRTC logic remains similar to previous implementation)
// ... but all API calls now include authentication headers

// Add logout button to HTML
document.addEventListener('DOMContentLoaded', () => {
    const logoutBtn = document.createElement('button');
    logoutBtn.textContent = 'Logout';
    logoutBtn.className = 'control-btn';
    logoutBtn.onclick = logout;
    document.querySelector('.controls').appendChild(logoutBtn);
});