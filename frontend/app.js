// Configuration
const API_CONFIG = {
    resultsApi: 'http://localhost:5001',
    bettingApi: 'http://localhost:5002',
    paymentsApi: 'http://localhost:5003'
};

// State management
let betslip = [];
let games = [];

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    loadGames();
    setupEventListeners();
});

// Load games from Results API
async function loadGames() {
    try {
        const response = await fetch(`${API_CONFIG.resultsApi}/api/jogos`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        games = await response.json();
        renderLiveGames();
        renderUpcomingGames();
    } catch (error) {
        console.error('Error loading games:', error);
        document.getElementById('live-games').innerHTML = '<div class="text-center text-red-500">Failed to load games</div>';
    }
}

// Render live games (estado = 2)
function renderLiveGames() {
    const liveContainer = document.getElementById('live-games');
    const liveGames = games.filter(g => g.estado === 2);
    
    if (liveGames.length === 0) {
        liveContainer.innerHTML = '<div class="text-center text-slate-400">No live games at the moment</div>';
        return;
    }

    liveContainer.innerHTML = liveGames.map(game => createLiveGameCard(game)).join('');
}

// Render upcoming games (estado = 1)
function renderUpcomingGames() {
    const upcomingContainer = document.getElementById('upcoming-games');
    const upcomingGames = games.filter(g => g.estado === 1).slice(0, 4);
    
    if (upcomingGames.length === 0) {
        upcomingContainer.innerHTML = '<div class="text-center text-slate-400 col-span-full">No upcoming games</div>';
        return;
    }

    upcomingContainer.innerHTML = upcomingGames.map(game => createUpcomingGameCard(game)).join('');
}

// Create live game card HTML
function createLiveGameCard(game) {
    const minute = Math.floor(Math.random() * 45); // Simulated minute
    const half = minute < 45 ? "1st Half" : "2nd Half";
    
    return `
        <div class="bg-surface-container hover:bg-surface-container-high transition-colors p-md rounded-lg border border-white/5 flex flex-col md:flex-row items-center justify-between gap-md">
            <div class="flex items-center gap-md flex-1">
                <div class="text-center min-w-[60px]">
                    <p class="text-orange-600 font-bold font-odds-display text-odds-display">${minute}'</p>
                    <p class="text-slate-500 font-label-caps text-[10px]">${half}</p>
                </div>
                <div class="space-y-xs flex-1">
                    <div class="flex justify-between items-center">
                        <span class="font-lexend font-bold text-white">${game.equipa_Casa}</span>
                        <span class="font-odds-display text-white">${game.golos_Casa}</span>
                    </div>
                    <div class="flex justify-between items-center">
                        <span class="font-lexend font-bold text-white">${game.equipa_Fora}</span>
                        <span class="font-odds-display text-white">${game.golos_Fora}</span>
                    </div>
                </div>
            </div>
            <div class="grid grid-cols-3 gap-xs w-full md:w-auto">
                <button onclick="addToBetslip(${game.id}, '${game.equipa_Casa}', 1.45, '${game.codigo_Jogo}')" class="bg-[#20252B] hover:bg-orange-600/20 active:bg-orange-600 transition-all p-4 rounded min-w-[100px] text-center border border-white/5 group">
                    <p class="text-[10px] text-slate-500 group-hover:text-slate-300 font-label-caps mb-1">HOME</p>
                    <p class="text-odds-display font-odds-display text-white">1.45</p>
                </button>
                <button onclick="addToBetslip(${game.id}, 'DRAW', 3.20, '${game.codigo_Jogo}')" class="bg-[#20252B] hover:bg-orange-600/20 active:bg-orange-600 transition-all p-4 rounded min-w-[100px] text-center border border-white/5 group">
                    <p class="text-[10px] text-slate-500 group-hover:text-slate-300 font-label-caps mb-1">DRAW</p>
                    <p class="text-odds-display font-odds-display text-white">3.20</p>
                </button>
                <button onclick="addToBetslip(${game.id}, '${game.equipa_Fora}', 6.80, '${game.codigo_Jogo}')" class="bg-[#20252B] hover:bg-orange-600/20 active:bg-orange-600 transition-all p-4 rounded min-w-[100px] text-center border border-white/5 group">
                    <p class="text-[10px] text-slate-500 group-hover:text-slate-300 font-label-caps mb-1">AWAY</p>
                    <p class="text-odds-display font-odds-display text-white">6.80</p>
                </button>
            </div>
        </div>
    `;
}

// Create upcoming game card HTML
function createUpcomingGameCard(game) {
    const gameTime = new Date(game.data_Hora_Inicio).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    
    return `
        <div class="bg-surface-container p-md rounded-lg border border-white/5 space-y-md">
            <div class="flex justify-between items-start">
                <span class="font-label-caps text-label-caps text-slate-500">${game.codigo_Jogo} • ${gameTime}</span>
                <span class="material-symbols-outlined text-slate-500 cursor-pointer hover:text-white">star</span>
            </div>
            <div class="flex items-center justify-between text-center">
                <div class="space-y-xs">
                    <div class="w-12 h-12 bg-slate-800 rounded-full mx-auto flex items-center justify-center border border-white/10">
                        <span class="material-symbols-outlined text-white">shield</span>
                    </div>
                    <p class="font-lexend font-bold text-white text-sm">${game.equipa_Casa}</p>
                </div>
                <span class="text-slate-600 font-black italic">VS</span>
                <div class="space-y-xs">
                    <div class="w-12 h-12 bg-slate-800 rounded-full mx-auto flex items-center justify-center border border-white/10">
                        <span class="material-symbols-outlined text-white">shield</span>
                    </div>
                    <p class="font-lexend font-bold text-white text-sm">${game.equipa_Fora}</p>
                </div>
            </div>
            <div class="flex gap-xs">
                <button onclick="addToBetslip(${game.id}, '${game.equipa_Casa}', 1.85, '${game.codigo_Jogo}')" class="flex-1 bg-[#20252B] p-3 rounded text-center border border-white/5 hover:bg-orange-600/20">
                    <p class="text-odds-display font-odds-display text-white">1.85</p>
                </button>
                <button onclick="addToBetslip(${game.id}, 'DRAW', 3.10, '${game.codigo_Jogo}')" class="flex-1 bg-[#20252B] p-3 rounded text-center border border-white/5 hover:bg-orange-600/20">
                    <p class="text-odds-display font-odds-display text-white">3.10</p>
                </button>
                <button onclick="addToBetslip(${game.id}, '${game.equipa_Fora}', 4.20, '${game.codigo_Jogo}')" class="flex-1 bg-[#20252B] p-3 rounded text-center border border-white/5 hover:bg-orange-600/20">
                    <p class="text-odds-display font-odds-display text-white">4.20</p>
                </button>
            </div>
        </div>
    `;
}

// Add selection to betslip
function addToBetslip(gameId, selection, odds, gameCode) {
    // Check if already in betslip
    if (betslip.find(b => b.gameId === gameId && b.selection === selection)) {
        alert('This selection is already in your betslip');
        return;
    }

    betslip.push({
        gameId,
        selection,
        odds,
        gameCode
    });

    updateBetslip();
}

// Remove from betslip
function removeFromBetslip(gameId, selection) {
    betslip = betslip.filter(b => !(b.gameId === gameId && b.selection === selection));
    updateBetslip();
}

// Update betslip display
function updateBetslip() {
    const count = betslip.length;
    const selectionCount = document.getElementById('selection-count');
    const selectionText = document.getElementById('selection-text');
    const betslipItems = document.getElementById('betslip-items');

    selectionCount.textContent = count;
    selectionText.textContent = count === 0 ? 'No selections' : `${count} Selection${count !== 1 ? 's' : ''}`;

    if (count === 0) {
        betslipItems.innerHTML = '<div class="text-center text-slate-500 py-8">Add selections to your betslip</div>';
    } else {
        betslipItems.innerHTML = betslip.map((bet, index) => createBetslipItem(bet, index)).join('');
    }

    updateOdds();
}

// Create betslip item HTML
function createBetslipItem(bet, index) {
    const game = games.find(g => g.id === bet.gameId);
    const matchText = game ? `${game.equipa_Casa} vs ${game.equipa_Fora}` : bet.gameCode;

    return `
        <div class="bg-slate-950 p-sm rounded-lg border border-orange-600/30 relative">
            <button onclick="removeFromBetslip(${bet.gameId}, '${bet.selection}')" class="absolute top-2 right-2 text-slate-500 hover:text-white">
                <span class="material-symbols-outlined text-sm">close</span>
            </button>
            <div class="flex items-center gap-xs mb-xs">
                <span class="material-symbols-outlined text-orange-600 text-sm" style="font-variation-settings: 'FILL' 1;">confirmation_number</span>
                <p class="text-orange-600 text-[10px] uppercase tracking-widest">Match Winner</p>
            </div>
            <p class="text-white font-bold text-sm mb-xs">${bet.selection}</p>
            <div class="flex justify-between items-center text-xs text-slate-500">
                <span>${matchText}</span>
                <span class="text-orange-600 font-black">${bet.odds.toFixed(2)}</span>
            </div>
        </div>
    `;
}

// Update odds calculation
function updateOdds() {
    const totalOdds = betslip.reduce((acc, bet) => acc * bet.odds, 1);
    const betAmount = parseFloat(document.getElementById('bet-amount').value) || 0;
    const potentialPayout = (betAmount * totalOdds).toFixed(2);

    document.getElementById('total-odds').textContent = totalOdds.toFixed(2);
    document.getElementById('potential-payout').textContent = `€${potentialPayout}`;
}

// Setup event listeners
function setupEventListeners() {
    document.getElementById('bet-amount').addEventListener('change', updateOdds);
    document.getElementById('bet-amount').addEventListener('input', updateOdds);
    
    document.getElementById('place-bet-btn').addEventListener('click', placeBet);

    // Refresh games every 5 seconds
    setInterval(loadGames, 5000);
}

// Place bet
async function placeBet() {
    if (betslip.length === 0) {
        alert('Please add selections to your betslip');
        return;
    }

    const betAmount = parseFloat(document.getElementById('bet-amount').value);
    if (betAmount <= 0) {
        alert('Please enter a valid bet amount');
        return;
    }

    try {
        // For now, just show a success message
        // In a real app, you would send this to the backend
        const totalOdds = betslip.reduce((acc, bet) => acc * bet.odds, 1);
        const payout = betAmount * totalOdds;

        alert(`✅ Bet placed successfully!\n\nAmount: €${betAmount}\nTotal Odds: ${totalOdds.toFixed(2)}\nPotential Payout: €${payout.toFixed(2)}`);
        
        // Clear betslip
        betslip = [];
        updateBetslip();
    } catch (error) {
        console.error('Error placing bet:', error);
        alert('Failed to place bet');
    }
}

// Auto-refresh odds display when bet amount changes
document.addEventListener('DOMContentLoaded', () => {
    const betAmountInput = document.getElementById('bet-amount');
    if (betAmountInput) {
        betAmountInput.addEventListener('input', updateOdds);
    }
});
