MathOCR Teacher Pro — C#/.NET 버전
=================================

이 버전에는 Python이 전혀 사용되지 않습니다.

최종 사용 형태
--------------
MathOCRTeacherPro.exe 더블클릭 → 바로 실행

최종 EXE를 self-contained 단일 파일로 publish하도록 설정되어 있으므로,
완성된 EXE를 사용하는 PC에는 Python도, .NET Runtime도 따로 설치할 필요가 없습니다.

현재 포함 기능
-------------
- Windows 10/11용 C# WinForms 프로그램
- PDF/JPG/PNG/BMP/WebP 열기
- Windows 내장 PDF 렌더러로 PDF 페이지 이미지화
- 페이지 이동
- 마우스로 문제/그림/해설 영역 직접 지정
- AI 자동 문제 영역 검출
- 문항 목록/답안 입력/삭제
- 수학 문제 AI OCR
- Word(.docx) 생성
- 한컴오피스 한글이 설치되어 있으면 DOCX → HWP 자동 저장 시도
- OpenAI API Key 로컬 저장
- Python 의존성 0

중요
----
이 압축파일은 '소스 프로젝트'입니다.
ChatGPT의 현재 작업 환경은 Windows가 아니어서 Windows EXE 자체를 여기서 컴파일할 수는 없습니다.

EXE를 만드는 방법은 2가지입니다.

방법 A — Windows 빌드 PC에서 1회 빌드
1. .NET 8 SDK가 설치된 Windows PC에서 BUILD_EXE.cmd 실행
2. 아래 폴더에 EXE가 생성됨:
   bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\MathOCRTeacherPro.exe
3. 생성된 EXE는 다른 Windows PC에서 Python 없이 실행 가능

방법 B — GitHub Actions
이 프로젝트에는 .github/workflows/build-windows.yml이 포함되어 있습니다.
GitHub에 프로젝트를 올린 뒤 Actions의 "Build Windows EXE"를 실행하면
Windows 클라우드에서 EXE를 빌드해 artifact로 받을 수 있습니다.
이 방법은 내 PC에 Python/.NET SDK를 설치하지 않아도 됩니다.

API
---
설정에서 OpenAI API Key와 모델명을 입력합니다.
기본 모델은 gpt-5.6입니다.
AI OCR 사용량에 따라 OpenAI API 사용료가 발생할 수 있습니다.

HWP
---
HWP 자동 저장은 '한컴오피스 한글'이 설치된 Windows에서만 동작합니다.
PC/한글 버전에 따라 COM 보안 설정 때문에 자동 변환이 실패할 수 있습니다.
이 경우 생성된 DOCX를 한글에서 열어 HWP로 저장할 수 있습니다.

저작권
------
특정 상용 MathOCR 프로그램의 코드, 아이콘, 이미지 리소스를 복제하지 않았습니다.
일반적인 OCR/문항 선택 작업 흐름을 독자적으로 구현했습니다.
