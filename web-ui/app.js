const appState = {
    user: { id: null, name: 'Convidado', email: '' },
    selectedGame: null,
    selectedBet: { tipo: '1', odd: 1.80 },
    jogos: [],
    apostas: [],
    transacoes: [],
    saldo: 0,
    lastSaldoUpdate: 'Ainda não atualizado'
};

const config = {
    bettingBase: 'http://localhost:5223/api',
    paymentsBase: 'http://localhost:5224/api',
    resultsBase: 'http://localhost:5221/api'
};

function getProp(obj, ...keys) {
    for (const key of keys) {
        if (obj && obj[key] !== undefined && obj[key] !== null) {
            return obj[key];
        }
    }
    return undefined;
}

function normalizeUser(user) {
    return {
        id: getProp(user, 'ID', 'id'),
        name: getProp(user, 'Nome', 'nome', 'Name', 'name') || '',
        email: getProp(user, 'Email', 'email') || ''
    };
}

function normalizeJogo(jogo) {
    return {
        Codigo_Jogo: getProp(jogo, 'Codigo_Jogo', 'codigo_Jogo', 'codigoJogo', 'codigo_jogo'),
        Data_Hora_Inicio: getProp(jogo, 'Data_Hora_Inicio', 'data_Hora_Inicio', 'dataHoraInicio', 'data_hora_inicio'),
        Equipa_Casa: getProp(jogo, 'Equipa_Casa', 'equipa_Casa', 'equipaCasa', 'equipa_casa'),
        Equipa_Fora: getProp(jogo, 'Equipa_Fora', 'equipa_Fora', 'equipaFora', 'equipa_fora'),
        Tipo_Competicao: getProp(jogo, 'Tipo_Competicao', 'tipo_Competicao', 'tipoCompeticao', 'tipo_competicao'),
        Estado: getProp(jogo, 'Estado', 'estado'),
        Odd_Casa: getProp(jogo, 'Odd_Casa', 'odd_Casa', 'oddCasa', 'odd_casa'),
        Odd_Empate: getProp(jogo, 'Odd_Empate', 'odd_Empate', 'oddEmpate', 'odd_empate'),
        Odd_Fora: getProp(jogo, 'Odd_Fora', 'odd_Fora', 'oddFora', 'odd_fora')
    };
}

function formatOdd(value, fallback) {
    const odd = parseFloat(value);
    return Number.isFinite(odd) ? odd.toFixed(2) : fallback.toFixed(2);
}

function normalizeAposta(aposta) {
    return {
        ID: getProp(aposta, 'ID', 'id'),
        Jogo_ID: getProp(aposta, 'Jogo_ID', 'jogo_ID', 'jogoId', 'jogo_id'),
        Codigo_Jogo: getProp(aposta, 'Codigo_Jogo', 'codigo_Jogo', 'codigoJogo', 'codigo_jogo'),
        Tipo_Aposta: getProp(aposta, 'Tipo_Aposta', 'tipo_Aposta', 'tipoAposta', 'tipo_aposta'),
        Valor_Apostado: getProp(aposta, 'Valor_Apostado', 'valor_Apostado', 'valorApostado', 'valor_apostado'),
        Odd_Momento: getProp(aposta, 'Odd_Momento', 'odd_Momento', 'oddMomento', 'odd_momento'),
        Estado: getProp(aposta, 'Estado', 'estado'),
        Data_Hora_Aposta: getProp(aposta, 'Data_Hora_Aposta', 'data_Hora_Aposta', 'dataHoraAposta', 'data_hora_aposta'),
        Equipa_Casa: getProp(aposta, 'Equipa_Casa', 'equipa_Casa', 'equipaCasa', 'equipa_casa'),
        Equipa_Fora: getProp(aposta, 'Equipa_Fora', 'equipa_Fora', 'equipaFora', 'equipa_fora'),
        Tipo_Competicao: getProp(aposta, 'Tipo_Competicao', 'tipo_Competicao', 'tipoCompeticao', 'tipo_competicao'),
        Data_Hora_Inicio: getProp(aposta, 'Data_Hora_Inicio', 'data_Hora_Inicio', 'dataHoraInicio', 'data_hora_inicio'),
        Golos_Casa: getProp(aposta, 'Golos_Casa', 'golos_Casa', 'golosCasa', 'golos_casa'),
        Golos_Fora: getProp(aposta, 'Golos_Fora', 'golos_Fora', 'golosFora', 'golos_fora'),
        Estado_Jogo: getProp(aposta, 'Estado_Jogo', 'estado_Jogo', 'estadoJogo', 'estado_jogo')
    };
}

function normalizeTransacao(tx) {
    return {
        Tipo_Transacao: getProp(tx, 'Tipo_Transacao', 'tipo_Transacao', 'tipoTransacao', 'tipo_transacao'),
        Valor: getProp(tx, 'Valor', 'valor'),
        Data_Hora: getProp(tx, 'Data_Hora', 'data_Hora', 'dataHora', 'data_hora'),
        Estado: getProp(tx, 'Estado', 'estado')
    };
}

function initApp() {
    setDefaultConfig();
    loadJogos();
    updateUserCard();
    renderApostas(appState.apostas);
    renderSaldo();
}

function showCreateUser() {
    document.getElementById('login-section').style.display = 'none';
    document.getElementById('create-section').style.display = 'grid';
}

function showLoginForm() {
    document.getElementById('login-section').style.display = 'grid';
    document.getElementById('create-section').style.display = 'none';
}

async function loginUser() {
    const email = document.getElementById('input-user-email').value.trim();
    if (!email) {
        showToast('Insira um email válido para entrar.', 'error');
        return;
    }

    try {
        const userRaw = await fetchJson(`${config.bettingBase}/utilizadores/by-email?email=${encodeURIComponent(email)}`);
        const user = normalizeUser(userRaw);
        appState.user = { id: user.id, name: user.name || email.split('@')[0], email };
        await refreshUserData();
        closeModal('modal-user');
        showToast('Bem-vindo, ' + appState.user.name + '!', 'success');
    } catch (error) {
        showToast(error.message || 'Não foi possível iniciar sessão.', 'error');
    }
}

async function createUser() {
    const name = document.getElementById('input-new-name').value.trim();
    const email = document.getElementById('input-new-email').value.trim();
    if (!name || !email) {
        showToast('Preencha nome e email para criar a conta.', 'error');
        return;
    }

    try {
        const result = await postJson(`${config.bettingBase}/utilizadores`, { nome: name, email });
        const createdUser = normalizeUser(result);
        appState.user = { id: createdUser.id || result.id, name, email };
        await refreshUserData();
        closeModal('modal-user');
        showToast('Conta criada com sucesso!', 'success');
    } catch (error) {
        showToast(error.message || 'Não foi possível criar a conta.', 'error');
    }
}

function logoutUser() {
    appState.user = { id: null, name: 'Convidado', email: '' };
    appState.saldo = 0;
    appState.apostas = [];
    appState.transacoes = [];
    updateUserCard();
    renderSaldo();
    renderApostas(appState.apostas);
    renderTransacoes();
    showToast('Sessão encerrada.', 'success');
}

function isLoggedIn() {
    return !!appState.user.email;
}

function updateUserCard() {
    document.getElementById('sidebar-user-name').textContent = appState.user.name || 'Convidado';
    document.getElementById('sidebar-saldo').textContent = isLoggedIn() ? appState.saldo.toFixed(2) + ' €' : '0.00 €';
    document.getElementById('sidebar-login-btn').style.display = isLoggedIn() ? 'none' : 'block';
    document.getElementById('sidebar-logout-btn').style.display = isLoggedIn() ? 'block' : 'none';
}

function openModal(id) {
    document.getElementById(id).classList.add('show');
}

function openBetModal() {
    if (!isLoggedIn()) {
        showToast('Inicie sessão para apostar.', 'error');
        return;
    }
    if (!appState.selectedGame) {
        showToast('Selecione um jogo antes de abrir a aposta.', 'error');
        return;
    }
    openModal('modal-aposta');
}

function closeModal(id) {
    document.getElementById(id).classList.remove('show');
}

function navTo(page, event) {
    document.querySelectorAll('.nav-item').forEach(btn => btn.classList.remove('active'));
    event.currentTarget.classList.add('active');
    document.querySelectorAll('.page').forEach(section => section.classList.remove('active'));
    document.getElementById('page-' + page).classList.add('active');
}

function filterJogos(status, element) {
    document.querySelectorAll('.filters .chip').forEach(chip => chip.classList.remove('active'));
    element.classList.add('active');
    const filtered = status === null ? appState.jogos : appState.jogos.filter(j => {
        if (status === 1) return j.Estado === 2;
        if (status === 2) return j.Estado === 3;
        return j.Estado === 1;
    });
    renderJogos(filtered);
}

function filterApostas(status, element) {
    document.querySelectorAll('#filtros-apostas .chip').forEach(chip => chip.classList.remove('active'));
    element.classList.add('active');
    const filtered = status === null ? appState.apostas : appState.apostas.filter(a => {
        if (status === 1) return a.estado === 'Pendente';
        if (status === 2) return a.estado === 'Ganha';
        if (status === 3) return a.estado === 'Perdida';
        return true;
    });
    renderApostas(filtered);
}

function getMarketLabel(teamName, fallback) {
    if (!teamName) return fallback;
    const short = teamName.split(' ')[0];
    return short.length <= 10 ? short : `${short.slice(0, 10)}...`;
}

function renderJogos(jogos) {
    const list = document.getElementById('jogos-list');
    list.innerHTML = jogos.map(jogo => {
        const statusLabel = mapEstadoToLabel(jogo.Estado);
        const startTime = new Date(jogo.Data_Hora_Inicio).toLocaleString('pt-PT', { hour: '2-digit', minute: '2-digit' });
        const oddCasa = formatOdd(jogo.Odd_Casa, 1.80);
        const oddEmpate = formatOdd(jogo.Odd_Empate, 3.20);
        const oddFora = formatOdd(jogo.Odd_Fora, 4.50);
        const homeLabel = getMarketLabel(jogo.Equipa_Casa, 'Casa');
        const awayLabel = getMarketLabel(jogo.Equipa_Fora, 'Fora');
        return `
        <article class="jogo-card" data-status="${statusLabel}">
            <div class="jogo-card-header">
                <div class="jogo-teams">
                    <div class="jogo-team">${jogo.Equipa_Casa}</div>
                    <div class="jogo-team away">${jogo.Equipa_Fora}</div>
                </div>
                <div class="jogo-meta">
                    <div class="jogo-league">${jogo.Tipo_Competicao || 'Competição indisponível'}</div>
                    <div class="jogo-details"><span>${startTime}</span></div>
                </div>
            </div>
            <div class="jogo-odds">
                <button class="btn odd-btn" onclick='selectJogo(${JSON.stringify(jogo.Codigo_Jogo)}, "1", ${oddCasa})'><span class="odd-market">${homeLabel}</span><span class="odd-odd">${oddCasa}</span></button>
                <button class="btn odd-btn" onclick='selectJogo(${JSON.stringify(jogo.Codigo_Jogo)}, "X", ${oddEmpate})'><span class="odd-market">Empate</span><span class="odd-odd">${oddEmpate}</span></button>
                <button class="btn odd-btn" onclick='selectJogo(${JSON.stringify(jogo.Codigo_Jogo)}, "2", ${oddFora})'><span class="odd-market">${awayLabel}</span><span class="odd-odd">${oddFora}</span></button>
            </div>
        </article>
    `;
    }).join('');
    document.getElementById('st-jogos').textContent = appState.jogos.length;
    document.getElementById('st-live').textContent = appState.jogos.filter(j => j.Estado === 2).length;
    document.getElementById('st-finished').textContent = appState.jogos.filter(j => j.Estado === 3).length;
    document.getElementById('st-upcoming').textContent = appState.jogos.filter(j => j.Estado === 1).length;
}

function selectJogo(codigo, betType, odd) {
    if (!isLoggedIn()) {
        showToast('Inicie sessão para apostar.', 'error');
        return;
    }

    const jogo = appState.jogos.find(j => j.Codigo_Jogo === codigo);
    if (!jogo) return;
    appState.selectedGame = jogo;
    appState.selectedBet = { tipo: betType, odd };
    const betName = betType === '1' ? 'Casa' : betType === 'X' ? 'Empate' : 'Fora';
    document.getElementById('modal-aposta-info').textContent = `${jogo.Equipa_Casa} vs ${jogo.Equipa_Fora} — ${betName}`;
    document.getElementById('input-aposta-valor').value = 10;
    document.getElementById('input-aposta-odd').value = odd;
    updateBetSummary();
    openModal('modal-aposta');
}

function updateBetSummary() {
    const valor = parseFloat(document.getElementById('input-aposta-valor').value) || 0;
    const odd = parseFloat(document.getElementById('input-aposta-odd').value) || 0;
    document.getElementById('sum-valor').textContent = valor ? `${valor.toFixed(2)} €` : '—';
    document.getElementById('sum-odd').textContent = odd ? odd.toFixed(2) : '—';
    document.getElementById('sum-premio').textContent = valor && odd ? `${(valor * odd).toFixed(2)} €` : '—';
}

async function submitBet() {
    if (!isLoggedIn()) {
        showToast('Inicie sessão para apostar.', 'error');
        return;
    }

    const valor = parseFloat(document.getElementById('input-aposta-valor').value);
    const odd = parseFloat(document.getElementById('input-aposta-odd').value);
    if (!valor || !odd || !appState.selectedGame) {
        showToast('Aposta incompleta. Escolha um jogo e insira os valores.', 'error');
        return;
    }

    try {
        await postJson(`${config.bettingBase}/apostas`, {
            codigo_Jogo: appState.selectedGame.Codigo_Jogo,
            utilizador_ID: appState.user.id,
            tipo_Aposta: appState.selectedBet.tipo || '1X2',
            valor_Apostado: valor,
            odd_Momento: odd
        });

        await refreshUserData();
        closeModal('modal-aposta');
        showToast('Aposta registada com sucesso!', 'success');
    } catch (error) {
        showToast(error.message || 'Erro ao registar a aposta.', 'error');
    }
}

function openDepositModal() {
    if (!isLoggedIn()) {
        showToast('Inicie sessão para depositar.', 'error');
        return;
    }
    const amount = parseFloat(document.getElementById('deposit-valor').value) || 20;
    document.getElementById('input-deposito').value = amount;
    openModal('modal-deposito');
}

async function submitDeposit() {
    const valor = parseFloat(document.getElementById('input-deposito').value);
    if (!valor || valor <= 0) {
        showToast('Insira um valor de depósito válido.', 'error');
        return;
    }

    try {
        await postJson(`${config.paymentsBase}/saldo/deposito`, {
            utilizador_ID: appState.user.id,
            valor
        });
        await refreshUserData();
        closeModal('modal-deposito');
        showToast('Depósito realizado com sucesso!', 'success');
    } catch (error) {
        showToast(error.message || 'Erro ao processar o depósito.', 'error');
    }
}

async function loadJogos() {
    try {
        const jogos = await fetchJson(`${config.resultsBase}/jogos`);
        appState.jogos = jogos.map(normalizeJogo);
        renderJogos(appState.jogos);
        showToast('Jogos carregados do DataGenerator/ResultsAPI.', 'success');
    } catch (error) {
        showToast(error.message || 'Não foi possível carregar os jogos.', 'error');
    }
}

async function loadSaldo() {
    if (!isLoggedIn()) {
        showToast('Inicie sessão para ver o saldo.', 'error');
        return;
    }

    try {
        const saldo = await fetchJson(`${config.paymentsBase}/saldo/${appState.user.id}`);
        appState.saldo = saldo.saldo;
        appState.lastSaldoUpdate = new Date().toLocaleTimeString('pt-PT');
        renderSaldo();
        updateUserCard();
        showToast('Saldo atualizado.', 'success');
    } catch (error) {
        showToast(error.message || 'Não foi possível carregar o saldo.', 'error');
    }
}

async function loadApostas() {
    if (!isLoggedIn()) return;
    try {
        const apostas = await fetchJson(`${config.bettingBase}/apostas?utilizadorId=${appState.user.id}`);
        appState.apostas = apostas.map(a => {
            const normalized = normalizeAposta(a);
            const stake = parseFloat(normalized.Valor_Apostado || 0);
            const oddValue = parseFloat(normalized.Odd_Momento || 0);
            return {
                id: normalized.ID,
                jogoId: normalized.Jogo_ID,
                codigoJogo: normalized.Codigo_Jogo,
                casa: normalized.Equipa_Casa || 'Casa',
                fora: normalized.Equipa_Fora || 'Fora',
                competencia: normalized.Tipo_Competicao || 'Competição',
                inicio: normalized.Data_Hora_Inicio ? new Date(normalized.Data_Hora_Inicio) : null,
                golosCasa: normalized.Golos_Casa ?? null,
                golosFora: normalized.Golos_Fora ?? null,
                estadoNumerico: normalized.Estado_Jogo ?? normalized.Estado,
                tipo: normalized.Tipo_Aposta || '1X2',
                valor: stake,
                valorFormatado: `${stake.toFixed(2)}€`,
                odd: oddValue.toFixed(2),
                potencial: (stake * oddValue) || 0,
                estado: mapApostaEstado(normalized.Estado),
                data: new Date(normalized.Data_Hora_Aposta).toLocaleDateString('pt-PT')
            };
        });
        renderApostas(appState.apostas);
    } catch (error) {
        showToast(error.message || 'Não foi possível carregar as apostas.', 'error');
    }
}

async function loadTransacoes() {
    if (!isLoggedIn()) return;
    try {
        const transacoes = await fetchJson(`${config.paymentsBase}/transacoes/${appState.user.id}`);
        appState.transacoes = transacoes.map(tx => {
            const normalized = normalizeTransacao(tx);
            return {
                tipo: normalized.Tipo_Transacao || 'Transação',
                valor: `${parseFloat(normalized.Valor || 0).toFixed(2)}€`,
                data: new Date(normalized.Data_Hora).toLocaleDateString('pt-PT'),
                estado: normalized.Estado || 'Concluído'
            };
        });
        renderTransacoes();
    } catch (error) {
        showToast(error.message || 'Não foi possível carregar as transações.', 'error');
    }
}

function formatGameScore(casa, fora, jogoEstado) {
    if (jogoEstado === 1) return '—';
    if (casa == null || fora == null) return '0 - 0';
    return `${casa} - ${fora}`;
}

function formatGameStateLabel(estado) {
    return estado === 2 ? 'Ao Vivo' : estado === 3 ? 'Finalizado' : 'Agendado';
}

function formatGameDate(data) {
    if (!data) return 'Sem data';
    return new Intl.DateTimeFormat('pt-PT', { dateStyle: 'short', timeStyle: 'short' }).format(data);
}

function renderApostas(apostas) {
    const apostasList = document.getElementById('apostas-list');
    apostasList.innerHTML = apostas.length > 0 ? apostas.map(aposta => `
        <article class="aposta-card">
            <div class="aposta-card-header">
                <div>
                    <div class="aposta-competition">${aposta.competencia}</div>
                    <div class="aposta-kickoff">${formatGameDate(aposta.inicio)}</div>
                </div>
                <span class="aposta-badge ${aposta.estado.toLowerCase()}">${aposta.estado}</span>
            </div>

            <div class="aposta-match">
                <div class="aposta-team home">${aposta.casa}</div>
                <div class="aposta-score">${formatGameScore(aposta.golosCasa, aposta.golosFora, aposta.estadoNumerico)}</div>
                <div class="aposta-team away">${aposta.fora}</div>
            </div>

            <div class="aposta-details">
                <div class="aposta-detail">
                    <span>Tipo</span>
                    <strong>${aposta.tipo}</strong>
                </div>
                <div class="aposta-detail">
                    <span>Aposta</span>
                    <strong>${aposta.valorFormatado}</strong>
                </div>
                <div class="aposta-detail">
                    <span>Odd</span>
                    <strong>${aposta.odd}</strong>
                </div>
                <div class="aposta-detail">
                    <span>Potencial</span>
                    <strong>${aposta.potencial.toFixed(2)}€</strong>
                </div>
            </div>

            <div class="aposta-meta">
                <span class="aposta-code">${aposta.codigoJogo || `#${aposta.jogoId}`}</span>
                <span class="aposta-game-state">${formatGameStateLabel(aposta.estadoNumerico)}</span>
            </div>
        </article>
    `).join('') : '<div class="empty-state">Nenhuma aposta encontrada. Faça uma aposta para ver o detalhe do jogo aqui.</div>';

    document.getElementById('st-total').textContent = apostas.length;
    document.getElementById('st-ganhas').textContent = apostas.filter(a => a.estado === 'Ganha').length;
    document.getElementById('st-perdidas').textContent = apostas.filter(a => a.estado === 'Perdida').length;
    document.getElementById('st-pendentes').textContent = apostas.filter(a => a.estado === 'Pendente').length;
}

function renderSaldo() {
    document.getElementById('saldo-valor').textContent = appState.saldo.toFixed(2) + ' €';
    document.getElementById('saldo-update').textContent = `Última atualização: ${appState.lastSaldoUpdate}`;
    renderTransacoes();
}

function renderTransacoes() {
    document.getElementById('transacoes-tbody').innerHTML = appState.transacoes.map(tx => `
        <tr>
            <td>${tx.tipo}</td>
            <td>${tx.valor}</td>
            <td>${tx.data}</td>
            <td>${tx.estado}</td>
        </tr>
    `).join('');
}

function setDefaultConfig() {
    document.getElementById('bettingBase').value = config.bettingBase;
    document.getElementById('paymentsBase').value = config.paymentsBase;
    document.getElementById('resultsBase').value = config.resultsBase;
}

function saveConfig() {
    config.bettingBase = document.getElementById('bettingBase').value.trim() || config.bettingBase;
    config.paymentsBase = document.getElementById('paymentsBase').value.trim() || config.paymentsBase;
    config.resultsBase = document.getElementById('resultsBase').value.trim() || config.resultsBase;
    showToast('Configurações guardadas localmente.', 'success');
    loadJogos();
    if (isLoggedIn()) {
        refreshUserData();
    }
}

function resetConfig() {
    config.bettingBase = 'http://localhost:5223/api';
    config.paymentsBase = 'http://localhost:5224/api';
    config.resultsBase = 'http://localhost:5221/api';
    setDefaultConfig();
    showToast('Configurações restauradas.', 'success');
    loadJogos();
    if (isLoggedIn()) {
        refreshUserData();
    }
}

async function refreshUserData() {
    await Promise.all([loadSaldo(), loadApostas(), loadTransacoes()]);
}

async function fetchJson(url) {
    const response = await fetch(url);
    if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || response.statusText || 'Erro na API');
    }
    return await response.json();
}

async function postJson(url, body) {
    const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || response.statusText || 'Erro na API');
    }
    return await response.json();
}

function mapEstadoToLabel(estado) {
    return estado === 2 ? 'Ao Vivo' : estado === 3 ? 'Finalizados' : 'Agendados';
}

function mapApostaEstado(estado) {
    return estado === 2 ? 'Ganha' : estado === 3 ? 'Perdida' : 'Pendente';
}

function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;
    document.getElementById('toasts').appendChild(toast);
    setTimeout(() => toast.remove(), 3500);
}

window.addEventListener('DOMContentLoaded', initApp);

window.updateBetSummary = updateBetSummary;
