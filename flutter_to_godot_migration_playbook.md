---
title: Flutter to Godot Migration Playbook
description: A structured mapping and strategy guide for migrating Flutter-based components to idiomatic Godot equivalents. Covers UI elements, logic patterns, asset pipelines, and project architecture.
---

# 🧭 Flutter → Godot Migration Playbook

This guide maps core Flutter concepts and structures into their Godot-native equivalents, ensuring consistency with retro aesthetic constraints and mobile performance targets.

---

## 📐 UI ELEMENTS

| Flutter Widget         | Godot Equivalent         | Notes |
|------------------------|--------------------------|-------|
| `Scaffold`             | `Control` + `Panel`      | Panel acts as root container for layout |
| `AppBar`               | `HBoxContainer` + `Label`| Add bevel + retro styling manually |
| `Text`                 | `Label` (BitmapFont)     | All fonts must use `.fnt` or `BitmapFont` |
| `ElevatedButton`       | `Button` + StyleBoxFlat  | Use Godot Themes for state changes |
| `Column` / `Row`       | `VBoxContainer` / `HBoxContainer` | Respect pixel grid spacing |
| `Stack`                | `Control` + Z-layering   | Use `z_index` or scene order |
| `ListView`             | `ScrollContainer` + `VBoxContainer` | For menus and logs |
| `AnimatedContainer`    | `Tween` + `Panel`        | Connect with `AnimationPlayer` if complex |

---

## 🧠 STATE MANAGEMENT

| Flutter Concept    | Godot Equivalent               |
|--------------------|--------------------------------|
| `setState()`       | Signals + `SceneTree` updates  |
| Provider/BLoC      | Autoload Singleton (e.g. `GameState.gd`) |
| `FutureBuilder`    | Signals or `await yield(...)`  |
| State Restoration  | Serialize in autoload (JSON)   |

---

## 🎮 INPUT & INTERACTION

| Flutter Input          | Godot Input Mapping              |
|------------------------|----------------------------------|
| `GestureDetector`      | `InputEventScreenTouch` / `Drag` |
| Tap/LongPress          | `pressed()`, `is_action_just_pressed()` |
| Focus handling         | `Focus` node + key signals       |

---

## 🎨 ASSETS & GRAPHICS

| Flutter Asset Type | Godot Import Rule                         |
|--------------------|------------------------------------------|
| PNG, SVG           | `filter = off`, `mipmaps = off`, `repeat = false` |
| Fonts (TTF)        | Convert to `.fnt` bitmap fonts only      |
| Animations         | Split into sprite sheets, use `AnimatedSprite2D` |

**Note:** All assets must align with retro fidelity rules (16-color palette, nearest filter).

---

## 🗂️ PROJECT STRUCTURE

| Flutter Folder         | Godot Equivalent        |
|------------------------|-------------------------|
| `lib/screens/`         | `scenes/`               |
| `lib/widgets/`         | `ui/` or `controls/`    |
| `lib/models/`          | `data/` or `scripts/`   |
| `main.dart`            | `main.tscn` + `main.gd` |

---

## 🔄 MIGRATION STRATEGY

1. **Inventory Existing Components**
   - List Flutter widgets, screens, services
   - Tag for complexity, animation, interaction depth

2. **Migrate by Domain**
   - Port UI skeleton (scenes)
   - Migrate state machines (GameState.gd)
   - Reconnect input patterns

3. **Validate**
   - Apply visual and input tests
   - Confirm palette and resolution rules
   - Run on target iPad device

---

## ✅ Migration Completion Checklist
- [ ] All UI rebuilt in `Control`-based scenes
- [ ] No Flutter dependencies remain
- [ ] Assets validated by `amiga-aesthetic-enforcer`
- [ ] Signals replace all imperative state calls
- [ ] Scene tree hierarchy clean and modular

---

You are no longer in a widget forest. You are in a scene tree under a dying light. Welcome home.

