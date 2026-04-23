## TimelinePlayer
TimelinePlayer is a WYSIWYG skill editing tool, let player can fast editing & preview chatacter action setting in unity timeline, then saved to serialized data which can be play in SequencePlayer.cs
SequencePlayer.cs is simple and light, easy to implement in any third-party FSM action or state

## Index
```
Assets/TimelinePlayer/
├── Player/          Player
│   └── SequencePlayer.cs
├── ReferenceHub/    CI
│   ├── ReferenceHub.cs
│   └── ReferenceEntry.cs
├── Timeline/        Timeline
│   ├── ActionClips/     Timeline action
│   ├── Data/            Timeline data
│   └── Track/           Custom Timeline 
└── Editor/          Editor
    ├── Exporter/        Exporter & Sync
    └── Inspector/       InspectorEditor
```
## Data flow
```
.playable (Unity Timeline)
      │
      ▼  Editor：Auto export when saving
TimelineAutoSync  →  SequenceExporter.BuildSequenceData()
      │
      ▼
MyCutscene_SequenceData.asset  (TimelineSequenceData)
      │
      ▼  Runtime：SequencePlayer.Play()
OnEnter → OnUpdate(normalizedTime) → OnExit / OnCancel
```
