MathOCR Teacher Pro v4.1 STABLE
=================================

이번 버전의 핵심
--------------
- Python 필요 없음
- Windows x64 self-contained EXE
- 시작 단계/실행 중 오류 자동 기록
- 오류 발생 시 startup-error.txt 생성
- 오른쪽 문항 패널 잘림 보완
- HWP로 변환 버튼 유지
- GitHub Actions에서 2종류 배포본 생성

GitHub에서 다시 빌드하는 방법
----------------------------
1. 이 ZIP을 압축 해제
2. GitHub 저장소의 기존 파일과 같은 위치에 v4.1 파일 업로드
3. .github/workflows/build-windows.yml도 교체
4. Actions > Build Windows EXE > Run workflow
5. 빌드 완료 후 Artifacts에서 다운로드

Artifacts가 2개 생깁니다.
------------------------
1) MathOCRTeacherPro-v4.1-SingleEXE
   - MathOCRTeacherPro.exe 하나
   - 먼저 이것을 사용하세요.

2) MathOCRTeacherPro-v4.1-FallbackFolder
   - 여러 파일이 들어 있는 폴더형 실행본
   - SingleEXE가 특정 PC에서 실행되지 않을 경우 이 버전을 사용하세요.
   - ZIP을 완전히 풀고 폴더 안 MathOCRTeacherPro.exe를 실행하세요.

프로그램이 또 안 켜질 경우
------------------------
같은 폴더에 startup-error.txt가 생성되도록 만들었습니다.
그 파일을 ChatGPT에 보내주면 정확한 시작 오류를 확인할 수 있습니다.

※ exe 폴더에 쓰기 권한이 없는 환경에서는
%LOCALAPPDATA%\\MathOCRTeacherPro\\startup-error.txt
위치에 기록될 수 있습니다.

HWP
---
한컴오피스 한글이 설치된 Windows PC에서 HWP 자동 저장을 시도합니다.
한글 COM 설정에 따라 실패할 수 있으며, 이 경우 DOCX는 정상 생성됩니다.
