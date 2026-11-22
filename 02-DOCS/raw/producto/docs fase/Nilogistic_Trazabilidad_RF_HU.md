# NILOGISTIC — Matriz de trazabilidad RF / RN ↔ Feature ↔ HU ↔ Pruebas

| Campo | Valor |
|---|---|
| Versión | 0.1 |
| Fecha | 05/10/2026 |
| Base | Fase 2 v1.4, Backlog v1.2, Fase 4 Lotes 1 y 2 |
| Cobertura de RF con HU | 55 de 145 (todos los de R1) |
| RF pendientes de HU | **90**, de R2, R3 y R4 (P-304) |

---

## 1. Resumen por release

| Release (del feature) | RF | Con HU | Pendientes de HU |
|---|---|---|---|
| R1 | 55 | 55 | 0 |
| R2 | 52 | 0 | 52 |
| R3 | 23 | 0 | 23 |
| R4 | 15 | 0 | 15 |
| **Total** | 145 | 55 | 90 |

Regla vigente: **las HU de R2 se aprueban con un sprint de anticipación**; las de R3 y R4 se refinan antes de planificar cada release.

---

## 2. Requerimientos funcionales

| RF | Épica | Feature | Release | HU | Pruebas | Estado |
|---|---|---|---|---|---|---|
| RF-PUB-01 | EP-01 | FT-012 | R1 | HU-040 | PU-HU-040-E1..E5 | Con HU (Lote 2) |
| RF-PUB-02 | EP-01 | FT-013 | R1 | HU-041 | PU-HU-041-E1..E4 | Con HU (Lote 2) |
| RF-PUB-03 | EP-01 | FT-014 | R1 | HU-042 | PU-HU-042-E1..E4 | Con HU (Lote 2) |
| RF-PUB-04 | EP-01 | FT-015 | R1 | HU-043 | PU-HU-043-E1..E8 | Con HU (Lote 2) |
| RF-PUB-05 | EP-01 | FT-016 | R1 | HU-044 | PU-HU-044-E1..E4 | Con HU (Lote 2) |
| RF-PUB-06 | EP-01 | FT-016 | R1 | HU-045 | PU-HU-045-E1..E5 | Con HU (Lote 2) |
| RF-PUB-07 | EP-01 | FT-017 | R1 | HU-046 | PU-HU-046-E1..E4 | Con HU (Lote 2) |
| RF-PUB-08 | EP-01 | FT-018 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-PUB-09 | EP-01 | FT-019 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-REG-01 | EP-02 | FT-020 | R1 | HU-025, HU-026 | PU-HU-025-E1..E6, PU-HU-026-E1..E5 | Con HU (Lote 1) |
| RF-REG-02 | EP-02 | FT-020 | R1 | HU-025 | PU-HU-025-E1..E6 | Con HU (Lote 1) |
| RF-REG-03 | EP-02 | FT-020 | R1 | HU-025 | PU-HU-025-E1..E6 | Con HU (Lote 1) |
| RF-REG-04 | EP-02 | FT-021 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-REG-05 | EP-02 | FT-022 | R1 | HU-027 | PU-HU-027-E1..E5 | Con HU (Lote 1) |
| RF-REG-06 | EP-02 | FT-022 | R1 | HU-028 | PU-HU-028-E1..E7 | Con HU (Lote 1) |
| RF-REG-07 | EP-02 | FT-022 | R1 | HU-029 | PU-HU-029-E1..E4 | Con HU (Lote 1) |
| RF-REG-08 | EP-02 | FT-023 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-AUT-01 | EP-03 | FT-024 | R1 | HU-014 | PU-HU-014-E1..E5 | Con HU (Lote 1) |
| RF-AUT-02 | EP-03 | FT-025 | R1 | HU-016 | PU-HU-016-E1..E5 | Con HU (Lote 1) |
| RF-AUT-03 | EP-03 | FT-025 | R1 | HU-017 | PU-HU-017-E1..E5 | Con HU (Lote 1) |
| RF-AUT-04 | EP-03 | FT-024 | R1 | HU-015 | PU-HU-015-E1..E5 | Con HU (Lote 1) |
| RF-AUT-05 | EP-03 | FT-026 | R1 | HU-018 | PU-HU-018-E1..E7 | Con HU (Lote 1) |
| RF-AUT-06 | EP-03 | FT-029 | R1 | HU-021, HU-022 | PU-HU-021-E1..E4, PU-HU-022-E1..E6 | Con HU (Lote 1) |
| RF-AUT-07 | EP-03 | FT-027 | R1 | HU-019, HU-020 | PU-HU-019-E1..E4, PU-HU-020-E1..E4 | Con HU (Lote 1) |
| RF-AUT-08 | EP-03 | FT-028 | R1 | HU-047 | PU-HU-047-E1..E7 | Con HU (Lote 2) |
| RF-AUT-09 | EP-03 | FT-030 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-AUT-10 | EP-03 | FT-030 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-AUT-11 | EP-03 | FT-029 | R1 | HU-033 | PU-HU-033-E1..E5 | Con HU (Lote 1) |
| RF-MEM-01 | EP-04 | FT-031 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-02 | EP-04 | FT-032 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-03 | EP-04 | FT-033 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-04 | EP-04 | FT-032 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-05 | EP-04 | FT-034 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-06 | EP-04 | FT-035 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-07 | EP-04 | FT-037 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-MEM-08 | EP-04 | FT-032 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-09 | EP-04 | FT-035 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-10 | EP-04 | FT-031 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-11 | EP-04 | FT-031 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-MEM-12 | EP-04 | FT-036 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-BLG-01 | EP-05 | FT-038 | R1 | HU-048 | PU-HU-048-E1..E7 | Con HU (Lote 2) |
| RF-BLG-02 | EP-05 | FT-038 | R1 | HU-049 | PU-HU-049-E1..E6 | Con HU (Lote 2) |
| RF-BLG-03 | EP-05 | FT-040 | R1 | HU-051 | PU-HU-051-E1..E5 | Con HU (Lote 2) |
| RF-BLG-04 | EP-05 | FT-041 | R1 | HU-052 | PU-HU-052-E1..E5 | Con HU (Lote 2) |
| RF-BLG-05 | EP-05 | FT-039 | R1 | HU-050 | PU-HU-050-E1..E4 | Con HU (Lote 2) |
| RF-BLG-06 | EP-05 | FT-042 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-BLG-07 | EP-05 | FT-043 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-BLG-08 | EP-05 | FT-044 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-NWS-01 | EP-06 | FT-045 | R1 | HU-053 | PU-HU-053-E1..E7 | Con HU (Lote 2) |
| RF-NWS-02 | EP-06 | FT-045 | R1 | HU-054 | PU-HU-054-E1..E7 | Con HU (Lote 2) |
| RF-NWS-03 | EP-06 | FT-046 | R1 | HU-055 | PU-HU-055-E1..E5 | Con HU (Lote 2) |
| RF-NWS-04 | EP-06 | FT-046 | R1 | HU-056 | PU-HU-056-E1..E5 | Con HU (Lote 2) |
| RF-NWS-05 | EP-06 | FT-047 | R1 | HU-057, HU-058 | PU-HU-057-E1..E6, PU-HU-058-E1..E4 | Con HU (Lote 2) |
| RF-NWS-06 | EP-06 | FT-048 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-NWS-07 | EP-06 | FT-049 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-NWS-08 | EP-06 | FT-049 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EVT-01 | EP-07 | FT-050 | R1 | HU-059 | PU-HU-059-E1..E6 | Con HU (Lote 2) |
| RF-EVT-02 | EP-07 | FT-051 | R1 | HU-060 | PU-HU-060-E1..E5 | Con HU (Lote 2) |
| RF-EVT-03 | EP-07 | FT-051 | R1 | HU-060 | PU-HU-060-E1..E5 | Con HU (Lote 2) |
| RF-EVT-04 | EP-07 | FT-052 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EVT-05 | EP-07 | FT-055 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EVT-06 | EP-07 | FT-053 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EVT-07 | EP-07 | FT-056 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-EVT-08 | EP-07 | FT-054 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-01 | EP-08 | FT-058 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-02 | EP-08 | FT-059 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-03 | EP-08 | FT-060 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-04 | EP-08 | FT-057 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-05 | EP-08 | FT-061 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-06 | EP-08 | FT-062 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-07 | EP-08 | FT-063 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-08 | EP-08 | FT-064 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-EMP-09 | EP-08 | FT-065 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-EMP-10 | EP-08 | FT-066 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-ADS-01 | EP-09 | FT-067 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADS-02 | EP-09 | FT-068 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADS-03 | EP-09 | FT-069 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADS-04 | EP-09 | FT-070 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADS-05 | EP-09 | FT-071 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ADS-06 | EP-09 | FT-072 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-ADS-07 | EP-09 | FT-067 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-CUR-01 | EP-10 | FT-073 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-02 | EP-10 | FT-074 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-03 | EP-10 | FT-075 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-04 | EP-10 | FT-075 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-05 | EP-10 | FT-076 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-06 | EP-10 | FT-077 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-07 | EP-10 | FT-078 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-CUR-08 | EP-10 | FT-079 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-CUR-09 | EP-10 | FT-080 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-ECO-01 | EP-11 | FT-081 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-02 | EP-11 | FT-081 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-03 | EP-11 | FT-082 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-04 | EP-11 | FT-082 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-05 | EP-11 | FT-037 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-06 | EP-11 | FT-083 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-07 | EP-11 | FT-084 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-08 | EP-11 | FT-085 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-09 | EP-11 | FT-083 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ECO-10 | EP-11 | FT-094 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ECO-11 | EP-11 | FT-087 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-ECO-12 | EP-11 | FT-088 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-PAG-01 | EP-12 | FT-089 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-02 | EP-12 | FT-090 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-03 | EP-12 | FT-091 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-04 | EP-12 | FT-091 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-05 | EP-12 | FT-093 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-06 | EP-12 | FT-095 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-PAG-07 | EP-12 | FT-092 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-PAG-08 | EP-12 | FT-096 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ADM-01 | EP-13 | FT-097 | R1 | HU-023 | PU-HU-023-E1..E8 | Con HU (Lote 1) |
| RF-ADM-02 | EP-13 | FT-097 | R1 | HU-024 | PU-HU-024-E1..E5 | Con HU (Lote 1) |
| RF-ADM-03 | EP-13 | FT-098 | R1 | HU-061 | PU-HU-061-E1..E5 | Con HU (Lote 2) |
| RF-ADM-04 | EP-13 | FT-100 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADM-05 | EP-13 | FT-103 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADM-06 | EP-13 | FT-104 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-ADM-07 | EP-13 | FT-099 | R1 | HU-062, HU-063 | PU-HU-062-E1..E14, PU-HU-063-E1..E6 | Con HU (Lote 2) |
| RF-ADM-08 | EP-13 | FT-101 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-ADM-09 | EP-13 | FT-102 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-AUD-01 | EP-14 | FT-005 | R1 | HU-008 | PU-HU-008-E1..E5 | Con HU (Lote 1) |
| RF-AUD-02 | EP-14 | FT-105 | R1 | HU-064 | PU-HU-064-E1..E6 | Con HU (Lote 2) |
| RF-AUD-03 | EP-14 | FT-105 | R1 | HU-064 | PU-HU-064-E1..E6 | Con HU (Lote 2) |
| RF-AUD-04 | EP-14 | FT-005 | R1 | HU-009 | PU-HU-009-E1..E4 | Con HU (Lote 1) |
| RF-AUD-05 | EP-14 | FT-106 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-NOT-01 | EP-15 | FT-006 | R1 | HU-010 | PU-HU-010-E1..E5 | Con HU (Lote 1) |
| RF-NOT-02 | EP-15 | FT-006 | R1 | HU-010 | PU-HU-010-E1..E5 | Con HU (Lote 1) |
| RF-NOT-03 | EP-15 | FT-107 | R1 | HU-030 | PU-HU-030-E1..E5 | Con HU (Lote 1) |
| RF-NOT-04 | EP-15 | FT-006 | R1 | HU-010 | PU-HU-010-E1..E5 | Con HU (Lote 1) |
| RF-NOT-05 | EP-15 | FT-108 | R1 | HU-065 | PU-HU-065-E1..E5 | Con HU (Lote 2) |
| RF-NOT-06 | EP-15 | FT-109 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-MIG-01 | EP-16 | FT-111 | R1 | HU-067 | PU-HU-067-E1..E6 | Con HU (Lote 2) |
| RF-MIG-03 | EP-16 | FT-111 | R1 | HU-067 | PU-HU-067-E1..E6 | Con HU (Lote 2) |
| RF-MIG-04 | EP-16 | FT-112 | R1 | HU-068 | PU-HU-068-E1..E5 | Con HU (Lote 2) |
| RF-MIG-05 | EP-16 | FT-110 | R1 | HU-066 | PU-HU-066-E1..E5 | Con HU (Lote 2) |
| RF-MIG-06 | EP-16 | FT-113 | R1 | HU-031 | PU-HU-031-E1..E6 | Con HU (Lote 1) |
| RF-MIG-07 | EP-16 | FT-110 | R1 | HU-066 | PU-HU-066-E1..E5 | Con HU (Lote 2) |
| RF-BI-01 | EP-BI | FT-114 | R1 | HU-069, HU-070 | PU-HU-069-E1..E5, PU-HU-070-E1..E5 | Con HU (Lote 2) |
| RF-BI-02 | EP-BI | FT-115 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-BI-03 | EP-BI | FT-103 | R2 | – | – | **Pendiente de HU** (R2) |
| RF-BI-04 | EP-BI | FT-104 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-BI-05 | EP-BI | FT-116 | R3 | – | – | **Pendiente de HU** (R3) |
| RF-BI-06 | EP-BI | FT-117 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-BI-07 | EP-BI | FT-118 | R4 | – | – | **Pendiente de HU** (R4) |
| RF-BI-08 | EP-BI | FT-115, FT-116 | R2, R3 | – | – | **Pendiente de HU** (R2) |
| RF-BI-09 | EP-BI | FT-115, FT-103, FT-104 | R2, R3 | – | – | **Pendiente de HU** (R2) |

---

## 3. Reglas de negocio

| RN | HU asociadas | Estado |
|---|---|---|
| RN-001 | HU-007, HU-019, HU-024 | Con HU |
| RN-002 | HU-007, HU-023, HU-049, HU-050, HU-061 | Con HU |
| RN-003 | HU-007, HU-023, HU-049, HU-061 | Con HU |
| RN-004 | HU-070 | Con HU |
| RN-005 | HU-009, HU-064 | Con HU |
| RN-010 | HU-028 | Con HU |
| RN-011 | HU-023, HU-028 | Con HU |
| RN-012 | HU-028 | Con HU |
| RN-013 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-014 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-015 | HU-034, HU-035 | Con HU |
| RN-016 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-017 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-018 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-019 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-020 | HU-048 | Con HU |
| RN-021 | HU-059 | Con HU |
| RN-022 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-023 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-024 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-025 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-026 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-027 | HU-028 | Con HU |
| RN-030 | HU-053, HU-057 | Con HU |
| RN-031 | HU-054, HU-057 | Con HU |
| RN-032 | HU-054, HU-057, HU-058, HU-065 | Con HU |
| RN-033 | HU-030, HU-057 | Con HU |
| RN-040 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-041 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-042 | HU-059 | Con HU |
| RN-043 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-044 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-045 | HU-034 | Con HU |
| RN-046 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-047 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-048 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-049 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-050 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-051 | HU-034 | Con HU |
| RN-052 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-053 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-054 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-055 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-056 | HU-039, HU-042 | Con HU |
| RN-057 | HU-069, HU-070 | Con HU |
| RN-058 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-064 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-065 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-066 | – | Sin HU en R1 (R2, R3 o transversal) |
| RN-067 | HU-022, HU-023, HU-033 | Con HU |
| RN-068 | HU-026, HU-028 | Con HU |
| RN-069 | HU-062 | Con HU |
| RN-070 | HU-047, HU-053, HU-054 | Con HU |
| RN-071 | HU-070 | Con HU |
| RN-060 | HU-008, HU-064 | Con HU |
| RN-061 | HU-021, HU-022 | Con HU |
| RN-062 | HU-019, HU-020 | Con HU |
| RN-063 | HU-023, HU-024 | Con HU |

---

## 4. Requerimientos no funcionales

| RNF | HU asociadas | Estado |
|---|---|---|
| RNF-SEG-01 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-SEG-02 | HU-012 | Con HU |
| RNF-SEG-03 | HU-013 | Con HU |
| RNF-SEG-04 | HU-013 | Con HU |
| RNF-SEG-05 | HU-005, HU-006 | Con HU |
| RNF-SEG-06 | HU-004 | Con HU |
| RNF-SEG-07 | HU-004 | Con HU |
| RNF-SEG-08 | HU-014, HU-016, HU-017 | Con HU |
| RNF-SEG-09 | HU-005 | Con HU |
| RNF-PRI-01 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-PRI-02 | HU-032, HU-044, HU-045 | Con HU |
| RNF-PRI-03 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-PRI-04 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-PRI-05 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-REN-01 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-REN-02 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-REN-03 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-REN-04 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-REN-05 | HU-069 | Con HU |
| RNF-SEO-01 | HU-068 | Con HU |
| RNF-SEO-02 | HU-068 | Con HU |
| RNF-DIS-01 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-DIS-02 | HU-006 | Con HU |
| RNF-DIS-03 | HU-002 | Con HU |
| RNF-DIS-04 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-DIS-05 | HU-003, HU-006 | Con HU |
| RNF-OBS-01 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-OBS-02 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-USA-01 | HU-036, HU-037 | Con HU |
| RNF-USA-02 | HU-036 | Con HU |
| RNF-USA-03 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-USA-04 | – | Verificación transversal (puertas G1 a G7b) |
| RNF-USA-05 | HU-071 | Con HU |
| RNF-MAN-01 | HU-001 | Con HU |
| RNF-MAN-02 | HU-001, HU-038 | Con HU |
| RNF-MAN-03 | HU-005 | Con HU |
| RNF-MAN-04 | HU-001, HU-005 | Con HU |
| RNF-MAN-05 | HU-002 | Con HU |
| RNF-MAN-06 | HU-007 | Con HU |
| RNF-MAN-07 | HU-007 | Con HU |
