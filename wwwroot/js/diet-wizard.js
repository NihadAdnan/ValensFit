/**
 * ValensFit — Dedicated Diet Wizard Navigation & Submission
 */
const DietWizard = (() => {
    let currentStep = 1;
    const totalSteps = 3;

    function init() {
        updateProgress();
    }

    function selectCard(element, fieldId, value) {
        const container = element.closest('.card-selector-grid');
        if (container) {
            container.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        }
        element.classList.add('selected');
        const input = document.getElementById(fieldId);
        if (input) input.value = value;
    }

    function toggleTag(element, tagValue) {
        element.classList.toggle('active');
    }

    function toggleOfficeLunch(checkbox) {
        const box = document.getElementById('dietOfficeLunchBox');
        if (box) {
            box.style.display = checkbox.checked ? 'block' : 'none';
        }
    }

    function setHeightUnit(unit) {
        const cmWrap = document.getElementById('dietHeightCmWrap');
        const ftWrap = document.getElementById('dietHeightFtWrap');
        const cmBtn = document.getElementById('dietUnitCmBtn');
        const ftBtn = document.getElementById('dietUnitFtBtn');
        const hidden = document.getElementById('dietHeightUnit');

        hidden.value = unit;
        if (unit === 'cm') {
            cmWrap.style.display = 'block';
            ftWrap.style.display = 'none';
            cmBtn.classList.add('active');
            ftBtn.classList.remove('active');
        } else {
            cmWrap.style.display = 'none';
            ftWrap.style.display = 'flex';
            cmBtn.classList.remove('active');
            ftBtn.classList.add('active');
        }
    }

    function setWeightUnit(unit) {
        const kgBtn = document.getElementById('dietUnitKgBtn');
        const lbBtn = document.getElementById('dietUnitLbBtn');
        const hidden = document.getElementById('dietWeightUnit');

        hidden.value = unit;
        if (unit === 'kg') {
            kgBtn.classList.add('active');
            lbBtn.classList.remove('active');
        } else {
            kgBtn.classList.remove('active');
            lbBtn.classList.add('active');
        }
    }

    function validateStep(step) {
        if (step === 1) {
            const name = document.getElementById('dietFirstName').value.trim();
            const msg = document.getElementById('dietNameValMsg');
            const input = document.getElementById('dietFirstName');
            if (!name) {
                input.classList.add('input-error');
                if (msg) msg.style.display = 'block';
                return false;
            }
            input.classList.remove('input-error');
            if (msg) msg.style.display = 'none';
            return true;
        }

        if (step === 2) {
            const age = parseInt(document.getElementById('dietAge').value, 10);
            const ageInput = document.getElementById('dietAge');
            const ageMsg = document.getElementById('dietAgeValMsg');
            if (isNaN(age) || age < 13 || age > 80) {
                ageInput.classList.add('input-error');
                if (ageMsg) ageMsg.style.display = 'block';
                return false;
            }
            ageInput.classList.remove('input-error');
            if (ageMsg) ageMsg.style.display = 'none';

            // Height validation
            const hUnit = document.getElementById('dietHeightUnit').value;
            const hMsg = document.getElementById('dietHeightValMsg');
            if (hUnit === 'cm') {
                const cm = parseFloat(document.getElementById('dietHeightCm').value);
                const cmInput = document.getElementById('dietHeightCm');
                if (isNaN(cm) || cm < 100 || cm > 250) {
                    cmInput.classList.add('input-error');
                    if (hMsg) hMsg.style.display = 'block';
                    return false;
                }
                cmInput.classList.remove('input-error');
            } else {
                const ft = parseFloat(document.getElementById('dietHeightFt').value);
                if (isNaN(ft) || ft < 3 || ft > 8) {
                    if (hMsg) hMsg.style.display = 'block';
                    return false;
                }
            }
            if (hMsg) hMsg.style.display = 'none';

            // Weight validation
            const weight = parseFloat(document.getElementById('dietWeight').value);
            const weightInput = document.getElementById('dietWeight');
            const weightMsg = document.getElementById('dietWeightValMsg');
            if (isNaN(weight) || weight < 25 || weight > 300) {
                weightInput.classList.add('input-error');
                if (weightMsg) weightMsg.style.display = 'block';
                return false;
            }
            weightInput.classList.remove('input-error');
            if (weightMsg) weightMsg.style.display = 'none';

            return true;
        }

        return true;
    }

    function goToStep(step) {
        for (let i = 1; i <= totalSteps; i++) {
            const el = document.getElementById(`dietStep${i}`);
            if (el) el.style.display = (i === step) ? 'block' : 'none';
        }
        currentStep = step;
        updateProgress();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function nextStep(current) {
        if (!validateStep(current)) return;
        goToStep(current + 1);
    }

    function updateProgress() {
        const fill = document.getElementById('dietProgressFill');
        if (fill) {
            const percent = ((currentStep - 1) / (totalSteps - 1)) * 100;
            fill.style.width = `${percent}%`;
        }

        document.querySelectorAll('#dietProgressBar .step-node').forEach(node => {
            const s = parseInt(node.dataset.step, 10);
            node.classList.remove('active', 'completed');
            if (s === currentStep) node.classList.add('active');
            else if (s < currentStep) node.classList.add('completed');
        });
    }

    async function submitDietPlan() {
        const activeTags = [];
        document.querySelectorAll('.diet-tag.active').forEach(t => activeTags.push(t.textContent.trim()));

        const payload = {
            FirstName: document.getElementById('dietFirstName').value.trim() || 'Friend',
            Gender: document.getElementById('dietGender').value,
            Age: parseInt(document.getElementById('dietAge').value, 10) || 25,
            HeightUnit: document.getElementById('dietHeightUnit').value,
            Height: document.getElementById('dietHeightUnit').value === 'cm'
                ? parseFloat(document.getElementById('dietHeightCm').value) || 175
                : parseFloat(document.getElementById('dietHeightFt').value) || 5,
            HeightInches: parseFloat(document.getElementById('dietHeightIn')?.value) || 0,
            WeightUnit: document.getElementById('dietWeightUnit').value,
            Weight: parseFloat(document.getElementById('dietWeight').value) || 70,
            ActivityLevel: document.getElementById('dietActivity').value,
            Goal: document.getElementById('dietGoal').value,
            MealStructure: document.getElementById('dietBreakfast').value,
            OfficeLunch: document.getElementById('dietOfficeLunchToggle').checked,
            OfficeLunchDescription: document.getElementById('dietOutsideLunchDesc')?.value || '',
            DietPreferences: activeTags,
            MonthlyBudget: parseFloat(document.getElementById('dietMonthlyBudget').value) || 7000,
            Country: 'Bangladesh',
            CityRegion: 'Dhaka',
            Currency: 'BDT',
            PlanMode: 'Diet',
            ExercisePreference: 'NoExercise'
        };

        // Show generating state
        document.getElementById('dietStep3').style.display = 'none';
        document.getElementById('dietProgressBar').style.display = 'none';
        document.getElementById('dietStepLoading').style.display = 'block';

        try {
            const resp = await fetch('/Plan/GenerateDiet', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (resp.ok) {
                const res = await resp.json();
                if (res.success && res.redirectUrl) {
                    window.location.href = res.redirectUrl;
                } else {
                    alert('Error generating diet plan.');
                    goToStep(3);
                }
            } else {
                alert('Server returned an error.');
                goToStep(3);
            }
        } catch (e) {
            console.error(e);
            alert('Failed to connect to server.');
            goToStep(3);
        }
    }

    document.addEventListener('DOMContentLoaded', init);

    return {
        selectCard,
        toggleTag,
        toggleOfficeLunch,
        setHeightUnit,
        setWeightUnit,
        goToStep,
        nextStep,
        submitDietPlan
    };
})();
