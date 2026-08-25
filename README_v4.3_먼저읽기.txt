MathOCR Teacher Pro v4.3 — Splitter Startup Fix
================================================

핵심 수정
---------
- SplitContainer의 Panel1MinSize / Panel2MinSize 강제 설정 완전 제거
- 폼이 실제로 열린 뒤 현재 폭을 기준으로 SplitterDistance 계산
- 분할 위치 계산 실패가 프로그램 시작을 막지 않도록 예외 무시
- startup-error.txt 기록 기능 유지
- Python 필요 없음

빌드
----
GitHub 저장소에 이 ZIP의 파일을 기존 파일과 같은 위치로 업로드한 뒤:
Actions > Build Windows EXE > Run workflow

Artifacts:
- MathOCRTeacherPro-v4.3-SingleEXE
- MathOCRTeacherPro-v4.3-FallbackFolder

먼저 SingleEXE를 실행하세요.
