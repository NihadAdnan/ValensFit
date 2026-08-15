/**
 * ValensFit — Wizard Engine & Strict Step Validation
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
        updateStepUI(0);

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

        // Clear validation errors on input
        ['FirstName', 'Age', 'Height', 'Weight'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.addEventListener('input', () => clearError(id));
            }
        });
    }

    function showError(fieldId, customMsg) {
        const input = document.getElementById(fieldId);
        const msgEl = document.getElementById(`valMsg_${fieldId}`);
        if (input) {
            input.classList.add('input-error');
            input.focus();
        }
        if (msgEl) {
            if (customMsg) msgEl.textContent = customMsg;
            msgEl.style.display = 'block';
        }
    }

    function clearError(fieldId) {
        const input = document.getElementById(fieldId);
        const msgEl = document.getElementById(`valMsg_${fieldId}`);
        if (input) input.classList.remove('input-error');
        if (msgEl) msgEl.style.display = 'none';
    }

    function goToStep(stepNumber) {
        currentStep = stepNumber;
        updateStepUI(stepNumber);
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function updateStepUI(stepNumber) {
        document.querySelectorAll('.wizard-step').forEach(el => {
            el.style.display = 'none';
            el.classList.remove('active');
        });

        const activeStepEl = document.getElementById(`step${stepNumber}`);
        if (activeStepEl) {
            activeStepEl.style.display = 'block';
            activeStepEl.classList.add('active');
        }

        const progressSection = document.getElementById('progressBarSection');
        const progressFill = document.getElementById('progressFill');

        if (stepNumber === 0 || stepNumber === 5) {
            if (progressSection) progressSection.style.display = 'none';
        } else {
            if (progressSection) progressSection.style.display = 'flex';
            const pct = ((stepNumber - 1) / 3) * 100;
            if (progressFill) progressFill.style.width = `${pct}%`;

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
        const nameVal = nameInput ? nameInput.value.trim() : '';

        if (!nameVal) {
            showError('FirstName', 'Please enter your first name.');
            return false;
        }

        clearError('FirstName');
        goToStep(2);
        return true;
    }

    function validateStep2() {
        let isValid = true;

        // 1. Age validation
        const ageInput = document.getElementById('Age');
        const age = parseInt(ageInput?.value, 10);
        if (isNaN(age) || age < 13 || age > 80) {
            showError('Age', 'Please enter an age between 13 and 80.');
            isValid = false;
        } else {
            clearError('Age');
        }

        // 2. Height validation
        const heightUnit = document.getElementById('HeightUnit')?.value || 'cm';
        let heightCm = 0;
        if (heightUnit === 'cm') {
            const h = parseFloat(document.getElementById('Height')?.value);
            if (isNaN(h) || h < 100 || h > 250) {
                showError('Height', 'Please enter a valid height (100–250 cm).');
                isValid = false;
            } else {
                clearError('Height');
                heightCm = h;
            }
        } else {
            const ft = parseFloat(document.getElementById('HeightFt')?.value);
            const inches = parseFloat(document.getElementById('HeightInches')?.value) || 0;
            if (isNaN(ft) || ft < 3 || ft > 8) {
                showError('Height', 'Please enter a valid height (3–8 ft).');
                isValid = false;
            } else {
                clearError('Height');
                heightCm = ((ft * 12) + inches) * 2.54;
            }
        }

        // 3. Weight validation
        const weightUnit = document.getElementById('WeightUnit')?.value || 'kg';
        const weightVal = parseFloat(document.getElementById('Weight')?.value);
        if (isNaN(weightVal) || (weightUnit === 'kg' && (weightVal < 25 || weightVal > 300)) || (weightUnit === 'lb' && (weightVal < 55 || weightVal > 660))) {
            showError('Weight', `Please enter a valid weight (${weightUnit === 'kg' ? '25–300 kg' : '55–660 lbs'}).`);
            isValid = false;
        } else {
            clearError('Weight');
        }

        // 4. Biological Sex check
        const gender = document.getElementById('Gender')?.value;
        if (!gender || (gender !== 'Male' && gender !== 'Female')) {
            document.getElementById('Gender').value = 'Male';
        }

        if (!isValid) return false;

        goToStep(3);
        return true;
    }

    function validateStep3() {
        const country = document.getElementById('Country')?.value;
        if (!country) {
            alert('Please select your country.');
            return false;
        }
        goToStep(4);
        return true;
    }

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

    function setMealStructure(structure, element) {
        document.getElementById('MealStructure').value = structure;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');
    }

    function toggleOfficeLunch(isChecked) {
        const detail = document.getElementById('officeLunchDetail');
        if (detail) {
            detail.style.display = isChecked ? 'block' : 'none';
        }
    }

    function setExercise(exercise, element) {
        document.getElementById('ExercisePreference').value = exercise;
        element.parentElement.querySelectorAll('.select-card').forEach(c => c.classList.remove('selected'));
        element.classList.add('selected');
    }

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

            const ft = parseFloat(document.getElementById('HeightFt').value) || 5;
            const inches = parseFloat(document.getElementById('HeightInches').value) || 9;
            const cm = Math.round(((ft * 12) + inches) * 2.54);
            document.getElementById('Height').value = cm;
        } else {
            btnFt.classList.add('active');
            btnCm.classList.remove('active');
            cmGrp.style.display = 'none';
            ftGrp.style.display = 'flex';

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

    function onCountryChange(countryName) {
        const info = countryCurrencyMap[countryName] || { currency: "USD", symbol: "$", defaultBudget: 300 };
        document.getElementById('Currency').value = info.currency;
        document.getElementById('currencyLabel').textContent = `${info.currency} (${info.symbol})`;
        document.getElementById('MonthlyBudget').value = info.defaultBudget;
    }

    function toggleDietTag(el, tag) {
        if (selectedDietTags.has(tag)) {
            selectedDietTags.delete(tag);
            el.classList.remove('active');
        } else {
            selectedDietTags.add(tag);
            el.classList.add('active');
        }
    }

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
                else c.classList.remove('selected');
            });
        }
        if (data.goal) {
            document.getElementById('Goal').value = data.goal;
            document.querySelectorAll('#step2 .select-card').forEach(c => {
                if (c.innerText.includes(data.goal)) c.classList.add('selected');
                else c.classList.remove('selected');
            });
        }
        if (data.country) {
            document.getElementById('Country').value = data.country;
            onCountryChange(data.country);
        }
        if (data.cityRegion) document.getElementById('CityRegion').value = data.cityRegion;
        if (data.monthlyBudget) document.getElementById('MonthlyBudget').value = data.monthlyBudget;
        if (data.officeLunch) {
            document.getElementById('OfficeLunch').checked = data.officeLunch;
            toggleOfficeLunch(true);
            if (data.officeLunchDescription) {
                document.getElementById('OfficeLunchDescription').value = data.officeLunchDescription;
            }
        }

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

    async function submitAndGenerate() {
        goToStep(5);

        animateGenStep('genStep1', 300);
        animateGenStep('genStep2', 600);
        animateGenStep('genStep3', 900);
        animateGenStep('genStep4', 1200);
        animateGenStep('genStep5', 1500);

        const heightVal = parseFloat(document.getElementById('Height')?.value) || 175;
        const weightVal = parseFloat(document.getElementById('Weight')?.value) || 70;
        const nameVal = document.getElementById('FirstName')?.value?.trim() || 'Friend';

        const payload = {
            firstName: nameVal,
            gender: document.getElementById('Gender')?.value || 'Male',
            age: parseInt(document.getElementById('Age')?.value, 10) || 25,
            height: heightVal,
            heightUnit: document.getElementById('HeightUnit')?.value || 'cm',
            heightInches: parseFloat(document.getElementById('HeightInches')?.value) || 0,
            weight: weightVal,
            weightUnit: document.getElementById('WeightUnit')?.value || 'kg',
            activityLevel: document.getElementById('ActivityLevel')?.value || 'ModeratelyActive',
            goal: document.getElementById('Goal')?.value || 'LoseFat',
            maximizeMuscleRetention: document.getElementById('MaximizeMuscleRetention')?.checked ?? true,
            targetWeightLossKg: parseFloat(document.getElementById('TargetWeightLossKg')?.value) || null,
            timeframeWeeks: parseInt(document.getElementById('TimeframeWeeks')?.value, 10) || null,
            country: document.getElementById('Country')?.value || 'Bangladesh',
            cityRegion: document.getElementById('CityRegion')?.value || 'Dhaka',
            monthlyBudget: parseFloat(document.getElementById('MonthlyBudget')?.value) || null,
            currency: document.getElementById('Currency')?.value || 'BDT',
            mealStructure: document.getElementById('MealStructure')?.value || 'Standard',
            officeLunch: document.getElementById('OfficeLunch')?.checked || false,
            officeLunchDescription: document.getElementById('OfficeLunchDescription')?.value || '',
            dietPreferences: Array.from(selectedDietTags),
            customRestrictions: document.getElementById('CustomRestrictions')?.value || '',
            exercisePreference: document.getElementById('ExercisePreference')?.value || 'Gym',
            dailyStepsTarget: parseInt(document.getElementById('DailyStepsTarget')?.value, 10) || 10000,
            daysPerWeek: parseInt(document.getElementById('DaysPerWeek')?.value, 10) || 4,
            minutesPerSession: parseInt(document.getElementById('MinutesPerSession')?.value, 10) || 45,
            experienceLevel: document.getElementById('ExperienceLevel')?.value || 'Beginner'
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
                setTimeout(() => {
                    window.location.href = '/Plan/Result';
                }, 1600);
            } else {
                const errData = await response.json().catch(() => ({}));
                console.error('Calculation server error:', errData);
                alert(`Calculation notice: ${errData.message || 'Please review your inputs.'}`);
                goToStep(2);
            }
        } catch (err) {
            console.error('Generation network failed:', err);
            window.location.href = '/Plan/Result';
        }
    }

    function animateGenStep(elementId, delayMs) {
        setTimeout(() => {
            const el = document.getElementById(elementId);
            if (el) {
                el.classList.remove('active');
                el.classList.add('done');
                const badge = el.querySelector('span:first-child');
                if (badge) badge.textContent = '✓';
            }
        }, delayMs);
    }

    document.addEventListener('DOMContentLoaded', init);

    return {
        goToStep,
        validateStep1,
        validateStep2,
        validateStep3,
        setGender,
        setActivity,
        setGoal,
        setMealStructure,
        toggleOfficeLunch,
        setExercise,
        setHeightUnit,
        setWeightUnit,
        onCountryChange,
        toggleDietTag,
        loadPreset,
        submitAndGenerate
    };
})();
