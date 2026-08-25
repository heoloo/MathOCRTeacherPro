MathOCR Teacher Pro v5.3 — HWP Primary
=======================================

핵심 변경
---------
- 'HWP로 변환' 버튼을 누르면 처음부터 *.hwp 저장창이 뜹니다.
- HWP 생성 성공 시 바로 한글에서 열지 물어봅니다.
- HWP 생성 실패 시 실패 이유를 그대로 보여줍니다.
- HWP 실패를 DOCX로 조용히 대체하지 않습니다.
- DOCX는 오른쪽 'DOCX 저장' 버튼을 눌렀을 때만 생성합니다.
- HWP는 기존 v5.2의 native EquationCreate 수식 개체 삽입 방식을 사용합니다.
- Python 필요 없음.

사용
----
PDF 열기 → 문제 지정/자동 인식 → HWP로 변환
→ 저장 위치 선택 → .hwp 직접 생성 → 한글에서 열기

빌드
----
GitHub 저장소에 파일 덮어쓰기 후:
Actions > Build Windows EXE > Run workflow

Artifact:
MathOCRTeacherPro-v5.3-HWP-Primary
