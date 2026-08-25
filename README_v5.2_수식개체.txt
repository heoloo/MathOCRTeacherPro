MathOCR Teacher Pro v5.2 — Native HWP Equation 강제 변환
========================================================

이번 수정의 핵심:
1. AI가 수식을 일반 text로 잘못 분류해도 그대로 두지 않습니다.
2. HWP 생성 직전에 수학 표현을 한 번 더 탐지합니다.
3. n, m, f(n), g(n), n^2, n², 2≤n≤10, f(n)=2g(n) 등을
   반드시 한컴 EquationCreate로 전달합니다.
4. EquationCreate가 성공하면 실제 '한글 수식 개체'가 됩니다.
5. 실패한 경우에만 내용 유실 방지를 위해 일반 텍스트로 되돌립니다.
6. LaTeX 원문 출력은 사용하지 않습니다.

확인 방법:
최종 .hwp에서 n², f(n), 2≤n≤10 등을 더블클릭하세요.
한글 수식 편집 창이 열리면 정상입니다.

중요:
DOCX 파일은 백업용이라 Word 수식 개체가 아닙니다.
수식 개체 확인은 반드시 프로그램이 직접 생성한 .hwp 파일에서 하세요.

빌드:
GitHub Actions > Build Windows EXE > Run workflow
Artifact: MathOCRTeacherPro-v5.2-NativeEquation
