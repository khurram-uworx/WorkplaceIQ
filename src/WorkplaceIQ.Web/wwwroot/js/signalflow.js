// SignalFlow — Real-time pipeline monitor + interactive UI
(function () {
    'use strict';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/pipeline')
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    const pipelineBtn = document.getElementById('iq-run-pipeline');
    const statusEl = document.getElementById('iq-pipeline-status');
    const progressEl = document.getElementById('iq-pipeline-progress');
    const logEl = document.getElementById('iq-pipeline-log');
    const modalEl = document.getElementById('iqReclassifyModal');
    const modal = modalEl ? new bootstrap.Modal(modalEl) : null;
    const signalSelect = document.getElementById('iq-reclassify-signal');
    const confirmBtn = document.getElementById('iq-reclassify-confirm');
    const noiseBtn = document.getElementById('iq-reclassify-noise-btn');

    let currentSignalNames = [];
    let reclassifyItemId = null;

    function appendLog(message, level) {
        if (!logEl) return;
        const icons = { info: '\u2139', success: '\u2713', warn: '\u26A0', error: '\u274C' };
        const line = document.createElement('div');
        line.className = 'log-' + (level || 'info');
        line.textContent = (icons[level] || '') + ' ' + message;
        logEl.appendChild(line);
        logEl.scrollTop = logEl.scrollHeight;
    }

    // --- Sidebar update ---
    function updateSidebar(signalCounts) {
        const sidebar = document.querySelector('.iq-signalflow__sidebar ul.nav');
        if (!sidebar) return;
        sidebar.innerHTML = '';
        for (const [signal, count] of Object.entries(signalCounts)) {
            const li = document.createElement('li');
            li.className = 'nav-item';
            li.innerHTML = '<a class="nav-link" href="/signalflow/signals/' + encodeURIComponent(signal) + '">' +
                '<span>&#128193; ' + signal + '</span>' +
                '<span class="badge bg-secondary rounded-pill">' + count + '</span></a>';
            sidebar.appendChild(li);
        }
    }

    function updateDashboard() {
        connection.invoke('GetDashboardData').catch(function (err) {
            console.error('GetDashboardData failed:', err);
        });
    }

    // --- Reclassification modal ---
    function showReclassifyModal(itemId) {
        if (!modal || !signalSelect) return;
        reclassifyItemId = itemId;
        signalSelect.innerHTML = '';
        for (const name of currentSignalNames) {
            const opt = document.createElement('option');
            opt.value = name;
            opt.textContent = name;
            signalSelect.appendChild(opt);
        }
        modal.show();
    }

    function closeReclassifyModal() {
        if (modal) modal.hide();
        reclassifyItemId = null;
    }

    if (confirmBtn && modalEl) {
        confirmBtn.addEventListener('click', function () {
            const signal = signalSelect ? signalSelect.value : '';
            if (!reclassifyItemId || !signal) return;
            connection.invoke('ReclassifyItem', reclassifyItemId, signal, false)
                .then(function () {
                    closeReclassifyModal();
                    updateDashboard();
                    removeItemCard(reclassifyItemId);
                })
                .catch(function (err) { alert('Reclassify failed: ' + err); });
        });
    }

    if (noiseBtn && modalEl) {
        noiseBtn.addEventListener('click', function () {
            if (!reclassifyItemId) return;
            connection.invoke('ReclassifyItem', reclassifyItemId, '', true)
                .then(function () {
                    closeReclassifyModal();
                    updateDashboard();
                    removeItemCard(reclassifyItemId);
                })
                .catch(function (err) { alert('Mark as noise failed: ' + err); });
        });
    }

    function removeItemCard(itemId) {
        const card = document.querySelector('[data-iq-item-id="' + itemId + '"]');
        if (card) {
            const itemCard = card.closest('.iq-signalflow__item-card');
            if (itemCard) itemCard.remove();
        }
    }

    // --- Unified pipeline event handler ---
    function handlePipelineEvent(evt) {
        switch (evt.type) {
            case 'started':
                if (statusEl) statusEl.textContent = 'Running\u2026 ' + evt.totalFeeds + ' feeds';
                if (progressEl) { progressEl.style.display = 'block'; }
                appendLog('Pipeline started with ' + evt.totalFeeds + ' feeds', 'info');
                break;

            case 'progress':
                if (statusEl) statusEl.textContent = evt.stage + ': ' + evt.current + '/' + evt.total;
                if (progressEl) {
                    const bar = progressEl.querySelector('.progress-bar');
                    if (bar) {
                        const pct = evt.total > 0 ? (evt.current / evt.total * 100) : 0;
                        bar.style.width = pct + '%';
                        bar.setAttribute('aria-valuenow', pct);
                    }
                }
                if (evt.message) appendLog(evt.message, 'info');
                break;

            case 'itemProcessed':
                const cls = evt.isNoise ? 'warn' : 'success';
                appendLog('[' + evt.signalOrNoise + '] ' + evt.title, cls);
                break;

            case 'failed':
                appendLog('FAIL [' + evt.stage + '] ' + evt.error, 'error');
                break;

            case 'completed':
                if (statusEl) statusEl.textContent = 'Idle';
                if (progressEl) { progressEl.style.display = 'none'; }
                appendLog('Done: ' + evt.totalItems + ' items, ' + evt.signalCount + ' signals, ' + evt.noiseCount + ' noise, ' + evt.failedCount + ' failed', 'success');
                if (pipelineBtn) { pipelineBtn.disabled = false; pipelineBtn.textContent = '\u25B6 Run Pipeline'; }
                updateDashboard();
                break;
        }
    }

    connection.on('pipelineEvent', handlePipelineEvent);

    // --- Dashboard data handler ---
    connection.on('dashboardData', function (data) {
        currentSignalNames = data.signalNames || [];
        updateSidebar(data.signalCounts || {});
    });

    // --- Reconnection: restore pipeline state if it was left running ---
    connection.on('pipelineState', function (state) {
        if (state.isRunning) {
            if (pipelineBtn) { pipelineBtn.disabled = true; pipelineBtn.textContent = 'Running\u2026'; }
            if (statusEl) statusEl.textContent = 'Running\u2026';
            if (state.lastEvent) {
                handlePipelineEvent(state.lastEvent);
            }
        }
    });

    // --- Connection start ---
    connection.start().then(function () {
        connection.invoke('GetPipelineState').catch(function (err) {
            console.error('GetPipelineState failed:', err);
        });
    }).catch(function (err) {
        console.error('SignalR connection error:', err);
    });

    // --- Pipeline trigger ---
    if (pipelineBtn) {
        pipelineBtn.addEventListener('click', function () {
            pipelineBtn.disabled = true;
            pipelineBtn.textContent = 'Running\u2026';
            if (logEl) logEl.innerHTML = '';
            if (statusEl) statusEl.textContent = 'Starting\u2026';
            connection.invoke('RunPipeline').catch(function (err) {
                console.error('RunPipeline failed:', err);
                appendLog('Failed to start pipeline: ' + err, 'error');
                pipelineBtn.disabled = false;
                pipelineBtn.textContent = '\u25B6 Run Pipeline';
            });
        });
    }

    // --- Item action buttons (reclassify, mark not noise, retry) ---
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-iq-action]');
        if (!btn) return;

        e.stopPropagation();

        const action = btn.getAttribute('data-iq-action');
        const itemId = btn.getAttribute('data-iq-item-id');
        if (!itemId) return;

        if (action === 'reclassify') {
            showReclassifyModal(itemId);
        } else if (action === 'mark-not-noise') {
            connection.invoke('MarkNotNoise', itemId)
                .then(function () {
                    updateDashboard();
                    removeItemCard(itemId);
                })
                .catch(function (err) { alert('Failed: ' + err); });
        } else if (action === 'retry') {
            connection.invoke('RetryItem', itemId)
                .then(function () {
                    updateDashboard();
                    removeItemCard(itemId);
                })
                .catch(function (err) { alert('Retry failed: ' + err); });
        }
    });
})();
