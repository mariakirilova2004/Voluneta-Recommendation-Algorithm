# 🧠 Voluneta Recommendation Algorithm

### Intelligent Corporate Volunteering Platform with Recommendation and Allocation Algorithms

Developed a custom recommendation system from scratch in **C#** without using any external machine learning frameworks for the application **Voluneta**.
---

## 🎯 Goal

Suggest to workers volunteering causes that are recommended on the basis of their profile and preference along with their availability, skills, and activities.

```text
Employee + Cause
       │
       ▼
  8 Input Features
       │
       ▼
   Dense Layer
       │
       ▼
      ReLU
       │
       ▼
Recommendation Score
```

---

## 🧩 Input Features

The following is the current model using 8 features:

| # | Feature |
|---|---|
| 1 | Interest Match |
| 2 | Category History Match |
| 3 | Day Availability Match |
| 4 | Time Availability Match |
| 5 | Location Match |
| 6 | Skill Match |
| 7 | Experience Match |
| 8 | Colleague Match |

---

## ✅ Current Progress

```text
Recommendation Feature Vector   ✅
Dense Layer                     ✅
ReLU Activation                 ✅
Forward Pass                    🔄
Output Layer                    ⏳
Loss Function                   ⏳
Backpropagation                 ⏳
Gradient Descent                ⏳
Model Training                  ⏳
Evaluation                      ⏳
Platform Integration            ⏳
```

---

## 🏗️ Project Structure

```text
Voluneta
│
├── Web
│
├── Voluneta.Recommendation.Core
│   ├── Models
│   ├── Algorithms
│   ├── Services
│   └── Data
│
└── Voluneta.Recommendation.Tests
```

---

## 🔬 Thesis Algorithms

### 1. Neural Recommendation Model
Custom C# implementation of a neural network.

Our current focus is on the Dense Layer, ReLU, and Forward Pass.

### 2. Capacity Constrained Allocation
An algorithm for assigning employees to causes which have a limited capacity.

```text
Recommendation Scores
        │
        ▼
 Employee-Cause Graph
        │
        ▼
 Capacity Constraints
        │
        ▼
 Min-Cost Max-Flow
```

A Greedy algorithm will be used for comparison.

---

## ⚙️ Technology

`C#` · `.NET 10` · `ASP.NET Core` · `xUnit` · `Azure SQL`

> The neural network is not implemented with the involvement of ML machine learning framework such as NET, TensorFlow or PyTorch.

---

## 👩‍💻 Author

**Maria Kirilova**  