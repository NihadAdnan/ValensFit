/**
 * ValensFit — Results Screen Interactivity & Dynamic Food Swapping
 */
const ResultView = (() => {
    let activeSwapContext = null;

    function init() {
        initCountUpAnimations();
    }

    // Smooth count-up numbers
    function initCountUpAnimations() {
        const counters = document.querySelectorAll('.count-up');
        counters.forEach(counter => {
            const target = parseFloat(counter.dataset.target) || 0;
            const isDecimal = target % 1 !== 0;
            const duration = 1200; // ms
            const startTime = performance.now();

            function update(currentTime) {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                // Ease out cubic
                const ease = 1 - Math.pow(1 - progress, 3);
                const currentVal = ease * target;

                if (isDecimal) {
                    counter.textContent = currentVal.toFixed(1);
                } else {
                    counter.textContent = Math.round(currentVal).toLocaleString();
                }

                if (progress < 1) {
                    requestAnimationFrame(update);
                }
            }

            requestAnimationFrame(update);
        });
    }

    // Day Tab Selection
    function selectDay(dayIndex, btnElement) {
        // Update tab buttons
        document.querySelectorAll('#dayTabs .day-tab-btn').forEach(btn => btn.classList.remove('active'));
        btnElement.classList.add('active');

        // Update day plan visibility
        document.querySelectorAll('.day-plan-card').forEach((card, idx) => {
            if (idx === dayIndex) {
                card.style.display = 'block';
                card.classList.add('active');
            } else {
                card.style.display = 'none';
                card.classList.remove('active');
            }
        });
    }

    // Interactive Food Swapping Modal
    async function openSwapModal(foodId, foodName, category, calories, protein, mealSlot, dayIndex) {
        activeSwapContext = { foodId, foodName, category, calories, protein, mealSlot, dayIndex };

        const modal = document.getElementById('swapModal');
        const swapText = document.getElementById('swapOriginalText');
        const selectEl = document.getElementById('swapFoodSelect');

        if (swapText) {
            swapText.innerHTML = `Replacing: <strong style="color: var(--gold-light);">${foodName}</strong> (${calories} kcal · ${protein}g P)`;
        }

        // Fetch food alternatives for this category
        try {
            selectEl.innerHTML = '<option>Loading alternatives...</option>';
            const resp = await fetch(`/Plan/GetFoodOptions?category=${encodeURIComponent(category)}`);
            if (resp.ok) {
                const foods = await resp.json();
                selectEl.innerHTML = '';
                foods.forEach(f => {
                    if (f.id !== foodId) {
                        const opt = document.createElement('option');
                        opt.value = f.id;
                        opt.textContent = `${f.name} (${f.caloriesPer100g} kcal/100g, ${f.proteinPer100g}g P)`;
                        selectEl.appendChild(opt);
                    }
                });
            }
        } catch (e) {
            console.error('Failed to load food options', e);
        }

        if (modal) modal.style.display = 'flex';
    }

    function closeSwapModal() {
        const modal = document.getElementById('swapModal');
        if (modal) modal.style.display = 'none';
        activeSwapContext = null;
    }

    async function executeFoodSwap() {
        if (!activeSwapContext) return;
        const selectEl = document.getElementById('swapFoodSelect');
        const newFoodId = selectEl.value;

        if (!newFoodId) {
            alert('Please choose a replacement food item.');
            return;
        }

        const payload = {
            targetFoodId: activeSwapContext.foodId,
            replacementFoodId: newFoodId,
            originalCalories: activeSwapContext.calories,
            originalProtein: activeSwapContext.protein,
            mealSlot: activeSwapContext.mealSlot
        };

        try {
            const response = await fetch('/Plan/SwapItem', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success && result.newItem) {
                    // Update DOM row in meal
                    const item = result.newItem;
                    const rowId = `itemRow_${activeSwapContext.foodId}`;
                    const rowEl = document.getElementById(rowId);

                    if (rowEl) {
                        rowEl.id = `itemRow_${item.foodId}`;
                        rowEl.innerHTML = `
                            <div class="food-name-qty">
                                <span style="color: var(--gold-light);">✦</span> ${item.foodName}
                                <span style="color: var(--text-muted); font-size: 0.85rem; margin-left: 0.4rem;">(${item.displayQuantity})</span>
                            </div>
                            <div class="food-macros-chips">
                                <span>${item.calories} kcal</span>
                                <span style="color: #68D391;">${item.protein} g P</span>
                                <span style="color: #63B3ED;">${item.carbs} g C</span>
                                <span style="color: #F6AD55;">${item.fat} g F</span>
                                <button type="button" class="food-swap-btn no-print" onclick="ResultView.openSwapModal('${item.foodId}', '${item.foodName}', '${item.category}', ${item.calories}, ${item.protein}, '${activeSwapContext.mealSlot}', ${activeSwapContext.dayIndex})">
                                    ⇄ Swap
                                </button>
                            </div>
                        `;
                    }
                    closeSwapModal();
                } else {
                    alert(result.message || 'Swap could not be completed.');
                }
            }
        } catch (err) {
            console.error('Swap request failed', err);
            alert('An error occurred during food swap.');
        }
    }

    // Grocery Checklist Item Toggle
    function toggleChecklistItem(checkbox) {
        const itemRow = checkbox.closest('.checklist-item');
        if (itemRow) {
            if (checkbox.checked) {
                itemRow.classList.add('checked');
            } else {
                itemRow.classList.remove('checked');
            }
        }
    }

    // Copy Grocery Checklist to Clipboard
    function copyGroceryList() {
        const items = document.querySelectorAll('.checklist-item');
        let text = '🏛️ VALENSFIT — 7-DAY GROCERY SHOPPING LIST\n========================================\n\n';

        items.forEach(item => {
            const label = item.querySelector('.checklist-label span')?.textContent?.trim() || '';
            const cost = item.querySelector('span:last-child')?.textContent?.trim() || '';
            text += `[ ] ${label} (${cost})\n`;
        });

        text += '\n========================================\nGenerated by ValensFit: Strength · Discipline · Vitality';

        navigator.clipboard.writeText(text).then(() => {
            alert('✓ 7-Day Grocery List copied to clipboard!');
        }).catch(() => {
            alert('Could not copy to clipboard. Please copy manually.');
        });
    }

    // Run on DOM load
    document.addEventListener('DOMContentLoaded', init);

    return {
        selectDay,
        openSwapModal,
        closeSwapModal,
        executeFoodSwap,
        toggleChecklistItem,
        copyGroceryList
    };
})();
