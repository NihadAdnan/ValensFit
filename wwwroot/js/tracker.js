/**
 * ValensFit — Daily Calorie Tracker & Food Logger Script
 */
const Tracker = (() => {

    function addFoodItem(slotId, key, name, qty, unit) {
        const listEl = document.querySelector(`#${slotId} .logged-items-list`);
        if (!listEl) return;

        const row = document.createElement('div');
        row.className = 'logged-item-row';
        row.dataset.key = key;
        row.dataset.name = name;
        row.dataset.qty = qty;
        row.dataset.unit = unit;
        row.innerHTML = `
            <span>• ${name} (${qty} ${unit})</span>
            <button type="button" class="remove-item-btn" onclick="Tracker.removeItem(this)">&times;</button>
        `;
        listEl.appendChild(row);
    }

    function removeItem(btn) {
        const row = btn.closest('.logged-item-row');
        if (row) row.remove();
    }

    function collectMealSlot(slotId, mealName) {
        const slotEl = document.getElementById(slotId);
        if (!slotEl) return null;

        const oilType = slotEl.querySelector('.track-oil-type')?.value || 'Mustard';
        const oilAmount = slotEl.querySelector('.track-oil-amount')?.value || 'Medium';
        const method = slotEl.querySelector('.track-method')?.value || 'Jhol';

        const items = [];
        slotEl.querySelectorAll('.logged-item-row').forEach(row => {
            items.push({
                FoodKey: row.dataset.key || '',
                CustomFoodName: row.dataset.name || '',
                Quantity: parseFloat(row.dataset.qty) || 1.0,
                PortionUnit: row.dataset.unit || 'serving'
            });
        });

        return {
            MealName: mealName,
            CookingOilType: oilType,
            OilAmount: oilAmount,
            CookingMethod: method,
            Items: items
        };
    }

    async function calculateIntake() {
        const bk = collectMealSlot('slotBreakfast', 'Breakfast');
        const ln = collectMealSlot('slotLunch', 'Lunch');
        const dn = collectMealSlot('slotDinner', 'Dinner');

        const meals = [];
        if (bk && bk.Items.length > 0) meals.push(bk);
        if (ln && ln.Items.length > 0) meals.push(ln);
        if (dn && dn.Items.length > 0) meals.push(dn);

        const payload = {
            Name: 'Athlete',
            WeightKg: parseFloat(document.getElementById('trackWeight')?.value) || 70,
            HeightCm: parseFloat(document.getElementById('trackHeight')?.value) || 172,
            Age: parseInt(document.getElementById('trackAge')?.value, 10) || 25,
            BiologicalSex: document.getElementById('trackSex')?.value || 'Male',
            ActivityLevel: document.getElementById('trackActivity')?.value || 'ModeratelyActive',
            CupsOfMilkTea: parseInt(document.getElementById('trackTeaCups')?.value, 10) || 0,
            SpoonsOfSugarPerCup: parseInt(document.getElementById('trackTeaSugar')?.value, 10) || 1,
            SnacksDescription: document.getElementById('trackSnackDesc')?.value || '',
            Meals: meals
        };

        try {
            const resp = await fetch('/Plan/CalculateCalories', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (resp.ok) {
                const res = await resp.json();
                if (res.success && res.redirectUrl) {
                    window.location.href = res.redirectUrl;
                } else {
                    alert('Error calculating calories.');
                }
            } else {
                alert('Server returned an error.');
            }
        } catch (e) {
            console.error(e);
            alert('Failed to connect to server.');
        }
    }

    return {
        addFoodItem,
        removeItem,
        calculateIntake
    };
})();
