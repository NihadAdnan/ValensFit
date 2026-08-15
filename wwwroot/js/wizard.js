/**
 * ValensFit — Roman Multi-Step Wizard Engine
 */
const Wizard = (() => {
    let currentStep = 0;
    const selectedDietTags = new Set(["Halal", "Egg + Chicken Only"]);

    const countryCurrencyMap = {
        "Bangladesh": { currency: "BDT", symbol: "৳", defaultBudget: 7000 },
        "United States": { currency: "USD", symbol: "$", defaultBudget: 350 },
        "India": { currency: "INR", symbol: "₹", defaultBudget: 5500 },
        "United Kingdom": { currency: "GBP", symbol: "£", defaultBudget: 220 },
        "European Union": { currency: "EUR", symbol: "€", defaultBudget: 260 },
        "Canada": { currency: "CAD", symbol: "C$", defaultBudget: 400 },
        "Australia": { currency: "AUD", symbol: "A$", defaultBudget: 450 }
    };

    function init() {
        // Setup initial step display
        updateStepUI(0);

        // Age listener for under-18 disclaimer
        const ageInput = document.getElementById('Age');
        if (ageInput) {
            ageInput.addEventListener('input', () => {
                const age = parseInt(ageInput.value, 10);
                const disclaimer = document.getElementById('ageDisclaimer');
                if (disclaimer) {
                    disclaimer.style.display = (age < 18) ? 'block' : 'none';
                }
            });
        }
    }

    function goToStep(stepNumber) {
        currentStep = stepNumber;
        updateStepUI(stepNumber);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function updateStepUI(stepNumber) {
        // Hide all steps
        document.querySelectorAll('.wizard-step').forEach(el => {
            el.style.display = 'none';
            el.classList.remove('active');
        });

        // Show active step
        const activeStepEl = document.getElementById(`step${stepNumber}`);
        if (activeStepEl) {
            activeStepEl.style.display = 'block';
            activeStepEl.classList.add('active');
        }

        // Progress bar visibility & percentage
        const progressSection = document.getElementById('progressBarSection');
        const progressFill = document.getElementById('progressFill');

        if (stepNumber === 0 || stepNumber === 5) {
            if (progressSection) progressSection.style.display = 'none';
        } else {
            if (progressSection) progressSection.style.display = 'flex';
            const pct = ((stepNumber - 1) / 3) * 100;
            if (progressFill) progressFill.style.width = `${pct}%`;

            // Update step nodes
            document.querySelectorAll('.step-node').forEach(node => {
                const nodeStep = parseInt(node.dataset.step, 10);
                node.classList.remove('active', 'completed');
                if (nodeStep === stepNumber) {
                    node.classList.add('active');
                } else if (nodeStep < stepNumber) {
                    node.classList.add('completed');
                }
            });
        }
    }

    function validateStep1() {
        const nameInput = document.getElementById('FirstName');
        if (!nameInput || !nameInput.value.trim()) {
            alert('Please enter your name, warrior.');
            nameInput?.focus();
            return;
        }
        goToStep(2);
    }

    function validateStep2() {
        const age = parseInt(document.getElementById('Age').value, 10);
        if (isNaN(age) || age < 13 || age > 80) {
            alert('Age must be between 13 and 80.');
            return;
        }
        goToStep(3);
    }

    function validateStep3() {
        goToStep(4);
    }

    // Selectable Card Helpers
    function setGender(gender, element) {
        document.getElementById('Gender').value = gender;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');
    }

    function setActivity(activity, element) {
        document.getElementById('ActivityLevel').value = activity;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');
    }

    function setGoal(goal, element) {
        document.getElementById('Goal').value = goal;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');

        const paceSec = document.getElementById('fatLossPaceSection');
        if (paceSec) {
            paceSec.style.display = (goal === 'LoseFat') ? 'block' : 'none';
        }
    }

    function setExercise(exercise, element) {
        document.getElementById('ExercisePreference').value = exercise;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');
    }

    // Unit Toggles
    function setHeightUnit(unit) {
        document.getElementById('HeightUnit').value = unit;
        const btnCm = document.getElementById('btnHeightCm');
        const btnFt = document.getElementById('btnHeightFt');
        const cmGrp = document.getElementById('heightCmGroup');
        const ftGrp = document.getElementById('heightFtGroup');

        if (unit === 'cm') {
            btnCm.classList.add('active');
            btnFt.classList.remove('active');
            cmGrp.style.display = 'block';
            ftGrp.style.display = 'none';

            // Auto-convert ft/in to cm if valid
            const ft = parseFloat(document.getElementById('HeightFt').value) || 5;
            const inches = parseFloat(document.getElementById('HeightInches').value) || 9;
            const cm = Math.round(((ft * 12) + inches) * 2.54);
            document.getElementById('Height').value = cm;
        } else {
            btnFt.classList.add('active');
            btnCm.classList.remove('active');
            cmGrp.style.display = 'none';
            ftGrp.style.display = 'flex';

            // Auto-convert cm to ft/in
            const cm = parseFloat(document.getElementById('Height').value) || 175;
            const totalInches = cm / 2.54;
            const ft = Math.floor(totalInches / 12);
            const inches = Math.round(totalInches % 12);
            document.getElementById('HeightFt').value = ft;
            document.getElementById('HeightInches').value = inches;
        }
    }

    function setWeightUnit(unit) {
        document.getElementById('WeightUnit').value = unit;
        const btnKg = document.getElementById('btnWeightKg');
        const btnLb = document.getElementById('btnWeightLb');
        const weightInput = document.getElementById('Weight');
        const currentVal = parseFloat(weightInput.value) || 70;

        if (unit === 'kg') {
            btnKg.classList.add('active');
            btnLb.classList.remove('active');
            if (currentVal > 120) {
                weightInput.value = Math.round(currentVal * 0.453592);
            }
        } else {
            btnLb.classList.add('active');
            btnKg.classList.remove('active');
            if (currentVal < 120) {
                weightInput.value = Math.round(currentVal * 2.20462);
            }
        }
    }

    // Country & Currency
    function onCountryChange(countryName) {
        const info = countryCurrencyMap[countryName] || { currency: "USD", symbol: "$", defaultBudget: 300 };
        document.getElementById('Currency').value = info.currency;
        document.getElementById('currencyLabel').textContent = `${info.currency} (${info.symbol})`;
        document.getElementById('MonthlyBudget').value = info.defaultBudget;
    }

    // Diet Tags
    function toggleDietTag(el, tag) {
        if (selectedDietTags.has(tag)) {
            selectedDietTags.delete(tag);
            el.classList.remove('active');
        } else {
            selectedDietTags.add(tag);
            el.classList.add('active');
        }
    }

    // Quick Presets
    async function loadPreset(presetKey) {
        try {
            const resp = await fetch(`/Plan/Preset?type=${encodeURIComponent(presetKey)}`);
            if (resp.ok) {
                const data = await resp.json();
                applyPresetData(data);
                goToStep(1);
            }
        } catch (e) {
            console.error('Failed to load preset', e);
        }
    }

    function applyPresetData(data) {
        if (data.firstName) document.getElementById('FirstName').value = data.firstName;
        if (data.gender) {
            document.getElementById('Gender').value = data.gender;
            document.querySelectorAll('#step2 .select-card').forEach(c => {
                if (c.innerText.includes(data.gender)) c.classList.add('selected');
                else c.classList.remove('selected');
            });
        }
        if (data.age) document.getElementById('Age').value = data.age;
        if (data.height) document.getElementById('Height').value = data.height;
        if (data.weight) document.getElementById('Weight').value = data.weight;
        if (data.activityLevel) {
            document.getElementById('ActivityLevel').value = data.activityLevel;
            document.querySelectorAll('#step2 .select-card').forEach(c => {
                if (c.innerText.includes(data.activityLevel)) c.classList.add('selected');
            });
        }
        if (data.goal) {
            document.getElementById('Goal').value = data.goal;
            document.querySelectorAll('#step2 .select-card').forEach(c => {
                if (c.innerText.includes(data.goal)) c.classList.add('selected');
            });
        }
        if (data.country) {
            document.getElementById('Country').value = data.country;
            onCountryChange(data.country);
        }
        if (data.cityRegion) document.getElementById('CityRegion').value = data.cityRegion;
        if (data.monthlyBudget) document.getElementById('MonthlyBudget').value = data.monthlyBudget;
        if (data.officeLunch) document.getElementById('OfficeLunch').checked = data.officeLunch;

        if (data.exercisePreference) {
            document.getElementById('ExercisePreference').value = data.exercisePreference;
            document.querySelectorAll('#step4 .select-card').forEach(c => {
                if (c.innerText.includes(data.exercisePreference)) c.classList.add('selected');
                else c.classList.remove('selected');
            });
        }
        if (data.daysPerWeek) document.getElementById('DaysPerWeek').value = data.daysPerWeek;
        if (data.minutesPerSession) document.getElementById('MinutesPerSession').value = data.minutesPerSession;
        if (data.experienceLevel) document.getElementById('ExperienceLevel').value = data.experienceLevel;

        // Diet tags
        selectedDietTags.clear();
        document.querySelectorAll('#dietTagsCloud .diet-tag').forEach(t => t.classList.remove('active'));
        if (data.dietPreferences) {
            data.dietPreferences.forEach(tag => {
                selectedDietTags.add(tag);
                document.querySelectorAll('#dietTagsCloud .diet-tag').forEach(t => {
                    if (t.dataset.tag === tag || t.innerText.toLowerCase().includes(tag.toLowerCase())) {
                        t.classList.add('active');
                    }
                });
            });
        }
    }

    // Submit and Generate Plan
    async function submitAndGenerate() {
        goToStep(5); // Show loading state

        // Animate generation steps
        animateGenStep('genStep1', 600);
        animateGenStep('genStep2', 1200);
        animateGenStep('genStep3', 1800);
        animateGenStep('genStep4', 2600);
        animateGenStep('genStep5', 3400);

        // Collect model payload
        const payload = {
            firstName: document.getElementById('FirstName').value.trim() || 'Athlete',
            gender: document.getElementById('Gender').value || 'Male',
            age: parseInt(document.getElementById('Age').value, 10) || 25,
            height: parseFloat(document.getElementById('Height').value) || 175,
            heightUnit: document.getElementById('HeightUnit').value || 'cm',
            heightInches: parseFloat(document.getElementById('HeightInches')?.value) || 0,
            weight: parseFloat(document.getElementById('Weight').value) || 70,
            weightUnit: document.getElementById('WeightUnit').value || 'kg',
            activityLevel: document.getElementById('ActivityLevel').value || 'ModeratelyActive',
            dailyStepsTarget: 8000,
            goal: document.getElementById('Goal').value || 'LoseFat',
            targetWeightLossKg: parseFloat(document.getElementById('TargetWeightLossKg')?.value) || null,
            timeframeWeeks: parseInt(document.getElementById('TimeframeWeeks')?.value, 10) || null,
            country: document.getElementById('Country').value || 'Bangladesh',
            cityRegion: document.getElementById('CityRegion').value || 'Dhaka',
            monthlyBudget: parseFloat(document.getElementById('MonthlyBudget').value) || null,
            currency: document.getElementById('Currency').value || 'BDT',
            dietPreferences: Array.from(selectedDietTags),
            customRestrictions: document.getElementById('CustomRestrictions').value || '',
            officeLunch: document.getElementById('OfficeLunch').checked,
            exercisePreference: document.getElementById('ExercisePreference').value || 'Gym',
            daysPerWeek: parseInt(document.getElementById('DaysPerWeek').value, 10) || 4,
            minutesPerSession: parseInt(document.getElementById('MinutesPerSession').value, 10) || 45,
            experienceLevel: document.getElementById('ExperienceLevel').value || 'Beginner'
        };

        try {
            const response = await fetch('/Plan/Generate', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                // Ensure at least 2.5s for smooth dramatic animation before redirecting to result
                setTimeout(() => {
                    window.location.href = '/Plan/Result';
                }, 2800);
            } else {
                alert('Calculation error occurred. Returning to review your inputs.');
                goToStep(4);
            }
        } catch (err) {
            console.error('Generation failed:', err);
            // Fallback: standard form submit
            document.getElementById('planForm').submit();
        }
    }

    function animateGenStep(elementId, delayMs) {
        setTimeout(() => {
            const el = document.getElementById(elementId);
            if (el) {
                el.classList.remove('active');
                el.classList.add('done');
                el.querySelector('span:first-child').textContent = '✓';
            }
        }, delayMs);
    }

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', init);

    return {
        goToStep,
        validateStep1,
        validateStep2,
        validateStep3,
        setGender,
        setActivity,
        setGoal,
        setExercise,
        setHeightUnit,
        setWeightUnit,
        onCountryChange,
        toggleDietTag,
        loadPreset,
        submitAndGenerate
    };
})();
