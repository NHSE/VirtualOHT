# 📦 **VirtualOHT**

> **SemiConductor-Equipment 프로그램과 연동하여 TCP/IP 기반의 PIO 통신 시나리오를 구현하고,  
> LP 내 Carrier의 Load / Unload 기능을 지원하는 가상 OHT 프로그램입니다.**

---

<p align="center">
  <img src="https://github.com/user-attachments/assets/70ffdf43-494f-4e90-a3bd-1a1594150a09" alt="OHT" width="850"/>
</p>

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

### ⚡ **PIO (Parallel I/O) Signal Definition**

| **Signal** | **Direction** | **Description** |
|:-------------:|:----------------:|:-------------------|
| **L_REQ** | P → A | LP 내부 Carrier 이동 가능 여부 |
| **READY** | P → A | Host가 장비의 Carrier Load / Unload 요청을 수락했음을 알림 |
| **CS_0** | A → P | Carrier Sensor #0 — LP1에서 Carrier 이송 가능 신호 |
| **CS_1** | A → P | Carrier Sensor #1 — LP2에서 Carrier 이송 가능 신호 |
| **VALID** | A → P | Carrier 이송 가능 상태 신호 |
| **TR_REQ** | A → P | Carrier 이송 준비 완료 신호 |
| **BUSY** | A → P | Carrier 이송 중임을 나타내는 신호 |
| **COMPT** | A → P | Carrier 이송 완료 신호 |
| **CONT** | A → P | - |
| **HO_AVBL** | P → A | - |
| **ES** | P → A | 이상(Error) 발생 신호 |
