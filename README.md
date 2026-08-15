# 🏛️ ValensFit — Personalized Diet & Fitness Plan Generator

> **"Strength · Discipline · Vitality"** — A free, zero-login, stateless diet & fitness plan generator engineered in **ASP.NET Core MVC (.NET 8)** with a dark Roman Marble & Gold aesthetic.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED.svg)](https://www.docker.com/)
[![Ollama](https://img.shields.io/badge/Ollama-Self--Hosted-black.svg)](https://ollama.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## ⚡ Key Highlights & Architecture

- **100% Free & Zero API Costs**: Core sports nutrition calculations (BMR, TDEE, macros, 7-day meal portion solving, workout schedules) are **100% deterministic C# algorithms**.
- **Ollama AI Grocery Grounding**: Local, self-hosted LLM (`llama3.2:1b` or `llama3.1:8b`) evaluates grocery budgets and cost-saving food swaps using real localized market price seeds (BDT, USD, INR, GBP, EUR).
- **Graceful Deterministic Fallback**: Strict 8–10s timeout — if Ollama is offline, the app seamlessly falls back to the deterministic price matrix without blocking.
- **Zero Friction & In-Memory Privacy**: No account creation, no passwords, no database writes. All biometrics exist solely in volatile memory during generation.
- **Dark Roman Aesthetic**: Deep marble black (`#0A0B0E`), Imperial Crimson (`#7A1F2B`), and Antique Gold (`#B08D57` / `#D4AF37`) with Roman motifs (Laurel wreath, columns, and stamped wax seals).

---

## 🛠️ Technology Stack

| Layer | Technology | Purpose |
|---|---|---|
| **Framework** | ASP.NET Core MVC (.NET 8) | High-performance server-side web application |
| **Styling** | Vanilla CSS + Roman Design Tokens | Responsive, dark theme with `@media print` support |
| **Typography** | Cinzel & Plus Jakarta Sans | Roman inscription headings + legible modern body |
| **AI Engine** | Ollama (Local / Containerized) | Scoped JSON budget grounding and food swap ideas |
| **Seed Data** | Curated JSON (`foods.json`, `workouts.json`, `prices.json`) | In-app nutrition matrix, exercises, and price indices |
| **Containerization** | Docker & Docker Compose | Multi-container setup for `web` and `ollama` |
| **CI/CD** | GitHub Actions | Automated build and Docker container packaging |

---

## 📐 Nutrition & Workout Engineering

1. **BMR Calculation**: Mifflin-St Jeor Equation:
   - *Male*: $BMR = 10 \times \text{weight} + 6.25 \times \text{height} - 5 \times \text{age} + 5$
   - *Female*: $BMR = 10 \times \text{weight} + 6.25 \times \text{height} - 5 \times \text{age} - 161$
2. **TDEE & Step Bonus**: Physical activity multiplier ($1.2\times$ to $1.9\times$) + dedicated step targets (e.g. 8k–10k steps) to prevent double counting.
3. **Macro Allocation**:
   - *Protein-First*: 1.8–2.2 g/kg (fat loss), 1.6–2.0 g/kg (hypertrophy), 1.4–1.8 g/kg (maintenance).
   - *Essential Fats*: 20–25% of total caloric expenditure.
   - *Complex Carbs*: Remainder calibrated for athletic fuel.
4. **Greedy Iterative Meal Solver**:
   - 7-day meal plan with non-repeating rotating food selections (e.g., lau $\rightarrow$ potol $\rightarrow$ spinach $\rightarrow$ cabbage $\rightarrow$ shim $\rightarrow$ pumpkin $\rightarrow$ broccoli).
   - Linear adjustment loop converging within $\pm 3\%$ of target calories and protein.
   - Strictly capped cooking oils ($\le 1-2$ tsp/meal) and zero added sugar.
5. **Safety Floors & Guardrails**:
   - Hard metabolic floors: never generates below 1,200 kcal/day (female) or 1,500 kcal/day (male).
   - Safe rate of weight loss validator (0.5%–0.75% bodyweight/week) with automatic warning/recalibration if user target is too aggressive.
   - Under-18 protective mode with capped deficit ($\le 10\%$).

---

## 🚀 Getting Started

### 1. Local Development (Without Docker)

Ensure [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) is installed:

```bash
# Clone the repository
git clone <your-repo-url>
cd ValensFit

# Run the ASP.NET Core application
dotnet run
```

Open your browser at `https://localhost:7000` or `http://localhost:5000`.

*(Optional: If you have Ollama running locally at `http://localhost:11434`, run `ollama pull llama3.2:1b` for AI budget analysis).*

---

### 2. Multi-Container Deployment (Docker Compose)

Deploy the entire stack with one command:

```bash
docker compose up -d --build
```

- Web application runs at: `http://localhost:8080`
- Ollama runs at: `http://localhost:11434`

Pull the recommended open-weight model into the Ollama container:
```bash
docker exec -it valensfit-ollama ollama pull llama3.2:1b
```

---

## 🏛️ UI/UX & Key Features

- **6-Step Interactive Wizard**: Guided multi-step flow with live unit toggles (cm/ft-in, kg/lb), selectable cards, and quick presets.
- **Count-Up Imperial Dashboard**: Live counting numbers for Daily Calories, Protein, Carbs, Fats, and Water hydration glasses.
- **Interactive Food Item Swapping**: Swap any protein, carb, or veggie on the results screen — the portion solver recalculates on-the-fly!
- **Shopping Cart Checklist**: Grouped grocery checklist by category with single-click clipboard export for WhatsApp / notes.
- **Imperial Training Scroll Print/PDF Export**: Dedicated high-contrast `@media print` layout formatting the entire plan as a printable parchment.

---

## 📄 License
This project is open-source under the [MIT License](LICENSE).
