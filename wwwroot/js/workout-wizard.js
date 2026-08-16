/**
 * ValensFit — Dedicated Workout Wizard Navigation & Submission
 */
const WorkoutWizard = (() => {
    let currentStep = 1;
    const totalSteps = 2;

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

    function setHeightUnit(unit) {
        const cmWrap = document.getElementById('wHeightCmWrap');
        const ftWrap = document.getElementById('wHeightFtWrap');
        const cmBtn = document.getElementById('wUnitCmBtn');
        const ftBtn = document.getElementById('wUnitFtBtn');
        const hidden = document.getElementById('workoutHeightUnit');

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
        const kgBtn = document.getElementById('wUnitKgBtn');
        const lbBtn = document.getElementById('wUnitLbBtn');
        const hidden = document.getElementById('workoutWeightUnit');

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
            const name = document.getElementById('workoutFirstName').value.trim();
            const msg = document.getElementById('workoutNameValMsg');
            const input = document.getElementById('workoutFirstName');
            if (!name) {
                input.classList.add('input-error');
                if (msg) msg.style.display = 'block';
                return false;
            }
            input.classList.remove('input-error');
            if (msg) msg.style.display = 'none';

            const age = parseInt(document.getElementById('workoutAge').value, 10);
            const ageInput = document.getElementById('workoutAge');
            const ageMsg = document.getElementById('workoutAgeValMsg');
            if (isNaN(age) || age < 13 || age > 80) {
                ageInput.classList.add('input-error');
                if (ageMsg) ageMsg.style.display = 'block';
                return false;
            }
            ageInput.classList.remove('input-error');
            if (ageMsg) ageMsg.style.display = 'none';

            const weight = parseFloat(document.getElementById('workoutWeight').value);
            const weightInput = document.getElementById('workoutWeight');
            const weightMsg = document.getElementById('workoutWeightValMsg');
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
            const el = document.getElementById(`workoutStep${i}`);
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
        const fill = document.getElementById('workoutProgressFill');
        if (fill) {
            const percent = ((currentStep - 1) / (totalSteps - 1)) * 100;
            fill.style.width = `${percent}%`;
        }

        document.querySelectorAll('#workoutProgressBar .step-node').forEach(node => {
            const s = parseInt(node.dataset.step, 10);
            node.classList.remove('active', 'completed');
            if (s === currentStep) node.classList.add('active');
            else if (s < currentStep) node.classList.add('completed');
        });
    }

    async function submitWorkoutPlan() {
        const payload = {
            FirstName: document.getElementById('workoutFirstName').value.trim() || 'Athlete',
            Gender: document.getElementById('workoutGender').value,
            Age: parseInt(document.getElementById('workoutAge').value, 10) || 25,
            HeightUnit: document.getElementById('workoutHeightUnit').value,
            Height: document.getElementById('workoutHeightUnit').value === 'cm'
                ? parseFloat(document.getElementById('workoutHeightCm').value) || 175
                : parseFloat(document.getElementById('workoutHeightFt').value) || 5,
            HeightInches: parseFloat(document.getElementById('workoutHeightIn')?.value) || 0,
            WeightUnit: document.getElementById('workoutWeightUnit').value,
            Weight: parseFloat(document.getElementById('workoutWeight').value) || 70,
            WorkoutGoal: document.getElementById('wGoal').value,
            ExercisePreference: document.getElementById('wPreference').value,
            DaysPerWeek: parseInt(document.getElementById('wDaysPerWeek').value, 10) || 4,
            MinutesPerSession: parseInt(document.getElementById('wMinutesPerSession').value, 10) || 45,
            ExperienceLevel: document.getElementById('wExperienceLevel').value,
            DailyStepsTarget: parseInt(document.getElementById('wDailySteps').value, 10) || 8000,
            PlanMode: 'Workout'
        };

        // Show loading state
        document.getElementById('workoutStep2').style.display = 'none';
        document.getElementById('workoutProgressBar').style.display = 'none';
        document.getElementById('workoutStepLoading').style.display = 'block';

        try {
            const resp = await fetch('/Plan/GenerateWorkout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (resp.ok) {
                const res = await resp.json();
                if (res.success && res.redirectUrl) {
                    window.location.href = res.redirectUrl;
                } else {
                    alert('Error generating workout plan.');
                    goToStep(2);
                }
            } else {
                alert('Server error generating workout.');
                goToStep(2);
            }
        } catch (e) {
            console.error(e);
            alert('Failed to connect to server.');
            goToStep(2);
        }
    }

    document.addEventListener('DOMContentLoaded', init);

    return {
        selectCard,
        setHeightUnit,
        setWeightUnit,
        goToStep,
        nextStep,
        submitWorkoutPlan
    };
})();
