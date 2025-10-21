# 📦 **VirtualOHT**

> **SemiConductor-Equipment 프로그램과 연동하여 TCP/IP 기반의 PIO 통신 시나리오를 구현하고,  
> LP 내 Carrier의 Load / Unload 기능을 지원하는 가상 OHT 프로그램입니다.**

---

<p align="center">
  <img src="https://github.com/user-attachments/assets/70ffdf43-494f-4e90-a3bd-1a1594150a09" alt="OHT" width="850"/>
</p>

---

## 🛠 설치 방법
### 1. 저장소 Clone 및 설치 방법

(git, dotnet이 설치가 되어 있지 않다면 설치 후 진행해주세요)

- git : [Download](https://git-scm.com/downloads)  
- dotnet : [Download](https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.304/dotnet-sdk-9.0.304-win-x64.exe)
```bash

git clone --branch main https://github.com/NHSE/VirtualOHT.git
cd VirtualOHT/VirtualOHT

2. 빌드
dotnet build

3. 실행
dotnet run
```
---

## ⚙️ **사용 방법**
🔗 [**Simulator 사용 가이드 보기**](https://github.com/NHSE/SemiConductor-Equipment/blob/master/docs/Simulator.md)

---

## 🏗️ **Architecture**

<p align="center">
  <img width="1567" height="898" alt="image" src="https://github.com/user-attachments/assets/57ac2d13-5e8f-439f-9158-ceb7abd55746" />
</p>

> **Client :** VirtualOHT  
> **Server :** SemiConductor-Equipment

---

## 🔌 **PIO 통신 시나리오**

<p align="center">
  <img width="558" height="674" alt="pio-sequence" src="https://github.com/user-attachments/assets/4c6032c3-e53e-48a4-92fc-8e775e7d3cf8" />
</p>

---
