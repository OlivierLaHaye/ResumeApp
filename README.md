# ResumeApp

A Windows WPF application that presents an interactive resume and portfolio with tab navigation, light and dark themes, and bilingual UI (en-CA / fr-CA).

## Overview

ResumeApp includes the following tabs:
- Resume (overview and contact)
- Experience
- Skills
- Projects (image carousels)
- Photography (albums with image carousels)
- Education

Theme and language can be changed from the UI and are remembered between launches.

## Requirements (users)

- Windows 10 or Windows 11
- A built Release build (for example `ResumeApp.exe`)

## How to run

1. Launch `ResumeApp.exe`.
2. Use the top controls to switch:
   - Theme: Light or Dark
   - Language: English (Canada) or French (Canada)
3. Navigate between tabs:
   Resume, Experience, Skills, Projects, Photography, Education

## Saved settings

ResumeApp saves:
- Theme (`Light` / `Dark`)
- Language (`EnglishCanada` / `FrenchCanada`)

These values are stored in the Windows registry:
- `HKEY_CURRENT_USER\Software\ResumeApp`
  - `Theme`
  - `Language`

### Reset settings

To reset to defaults:
- Remove `Theme` and `Language` (or delete the `ResumeApp` key) under `HKCU\Software\ResumeApp`
- Relaunch the application

## Build from source (developers)

### Prerequisites

- Visual Studio 2019 or 2022
- Workload: .NET desktop development
- Targeting pack: .NET Framework 4.8

### Build

1. Open the solution in Visual Studio.
2. Build in Debug or Release.

### Pre-build step (ResxCleaner.exe)

The project uses a pre-build event that calls `ResxCleaner.exe`.

If you do not have `ResxCleaner.exe` at the expected location, builds will fail. Options:
- Provide the executable in the project directory, or
- Disable the pre-build event in Project Properties > Build Events.

## Customizing content

### Text and localization

The UI uses resource keys (`.resx`) and a runtime language switch.

To update content:
- Edit the resource files (English and French).
- Keep the same keys in both languages.

### Images (Projects and Photography)

Projects and albums display images through a carousel control.

To add or update images:
- Place images under the appropriate `Resources\...` folders.
- Ensure files are included as Resources in the project file and copied to output if needed.

## Troubleshooting

### Build fails with “ResxCleaner.exe not found”
- Provide `ResxCleaner.exe` or disable the pre-build event.

### Theme or language is stuck on a previous choice
- Clear `HKCU\Software\ResumeApp` and restart the app.

## License

No license is included yet. Add a `LICENSE` file if you want to define usage and redistribution terms.

---

# ResumeApp (Français)

Application Windows WPF qui présente un CV et un portfolio interactifs avec navigation par onglets, thème clair et sombre, et interface bilingue (en-CA / fr-CA).

## Aperçu

ResumeApp inclut les onglets suivants :
- CV (aperçu et contact)
- Expérience
- Compétences
- Projets (carrousels d’images)
- Photographie (albums avec carrousels d’images)
- Éducation

Le thème et la langue peuvent être changés dans l’interface et sont mémorisés entre les ouvertures.

## Prérequis (utilisateurs)

- Windows 10 ou Windows 11
- Une build Release (par exemple `ResumeApp.exe`)

## Démarrage

1. Lance `ResumeApp.exe`.
2. Utilise les contrôles en haut pour changer :
   - Thème : clair ou sombre
   - Langue : anglais (Canada) ou français (Canada)
3. Navigue entre les onglets :
   CV, Expérience, Compétences, Projets, Photographie, Éducation

## Préférences sauvegardées

ResumeApp sauvegarde :
- Le thème (`Light` / `Dark`)
- La langue (`EnglishCanada` / `FrenchCanada`)

Ces valeurs sont stockées dans le registre Windows :
- `HKEY_CURRENT_USER\Software\ResumeApp`
  - `Theme`
  - `Language`

### Réinitialiser les préférences

Pour revenir aux valeurs par défaut :
- Supprime `Theme` et `Language` (ou la clé `ResumeApp`) sous `HKCU\Software\ResumeApp`
- Relance l’application

## Compilation (développeurs)

### Prérequis

- Visual Studio 2019 ou 2022
- Workload : .NET desktop development
- Targeting pack : .NET Framework 4.8

### Build

1. Ouvre la solution dans Visual Studio.
2. Compile en Debug ou Release.

### Étape pre-build (ResxCleaner.exe)

Le projet utilise un événement pre-build qui appelle `ResxCleaner.exe`.

Si tu n’as pas `ResxCleaner.exe` au bon emplacement, la compilation va échouer. Options :
- Fournir l’exécutable dans le répertoire du projet, ou
- Désactiver l’événement pre-build dans Project Properties > Build Events.

## Personnaliser le contenu

### Textes et traduction

L’interface utilise des clés de ressources (`.resx`) et un changement de langue à l’exécution.

Pour modifier le contenu :
- Mets à jour les fichiers de ressources (anglais et français).
- Conserve les mêmes clés dans les deux langues.

### Images (Projets et Photographie)

Les projets et les albums affichent des images via un contrôle de carrousel.

Pour ajouter ou mettre à jour des images :
- Place les images dans les dossiers `Resources\...` appropriés.
- Assure-toi que les fichiers sont inclus comme Resources dans le projet et copiés au besoin.

## Dépannage

### La compilation échoue avec “ResxCleaner.exe not found”
- Fournis `ResxCleaner.exe` ou désactive l’événement pre-build.

### Thème ou langue bloqués sur un ancien choix
- Efface `HKCU\Software\ResumeApp` puis relance l’app.

## Licence

Aucune licence n’est incluse pour l’instant. Ajoute un fichier `LICENSE` si tu veux définir les conditions d’utilisation et de redistribution.
