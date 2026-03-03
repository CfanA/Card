# CardGame  
### Slay the Spire 风格 Roguelike 卡牌构筑游戏（Unity）

> 使用 **Unity 2022.3 LTS** 开发  
> 单人回合制 Roguelike 卡牌构筑游戏  
> 教学型架构设计 + 可复现随机系统 + 事件驱动框架

---

## 项目简介

本项目参考《杀戮尖塔》的核心玩法结构，  
尽量实现**可扩展、可维护、可复现**的 Roguelike 卡牌框架。

---

## 当前版本功能

- 程序化地图生成（分层 DAG）
- 路线选择与锁定机制
- 回合制战斗系统
- 卡牌构筑与升级系统
- Buff / Debuff 状态机制
- 遗物系统（事件驱动）
- 药水系统（战斗中使用）
- 商店系统（买卡 / 买遗物 / 移除牌）
- 篝火（恢复 / 升级）
- Boss 战与结算界面
- 完整 Run 流程（从起点到通关）

---

## 项目架构

```text
Runtime
 ├── Core/         # RunState、流程控制、RNG
 ├── Map/          # 地图生成与渲染
 ├── Battle/       # 战斗系统与状态机制
 ├── Content/      # 卡牌/遗物/药水/事件定义
 ├── Rooms/        # 各类房间控制器
 └── Bootstrap/    # 启动入口
```

### 架构原则

- 系统分层清晰（Map / Battle / Content / Rooms 解耦）
- 数据与逻辑分离
- 事件总线统一管理战斗触发
- 所有随机基于 runSeed
- 不依赖 Unity 全局随机状态

---

## 地图系统

### 核心特性

- 分层有向无环图（DAG）
- 支持固定 seed 复现
- 路线锁定机制
- 节点状态管理：
  - Current
  - Available
  - Locked
  - Visited

### 设计原则

- 地图逻辑与渲染解耦
- MapProgression 负责可选路径计算
- 点击节点不会重新生成地图
- 自动相机适配与滚动控制

---

##  战斗系统

### 战斗流程

```text
StartBattle
→ PlayerTurn
→ EnemyTurn
→ Victory / Defeat
```

### 核心机制

- 抽牌堆 / 手牌 / 弃牌堆
- 能量系统
- Buff / Debuff（Strength / Weak / Vulnerable）
- 明确的伤害计算顺序
- 统一 BattleEventBus

### 事件驱动机制

遗物通过监听：

- OnBattleStart
- OnTurnStart
- OnPlayCard
- OnVictory

实现完全解耦扩展。

---

## 卡牌系统

- ScriptableObject 数据驱动
- 支持升级版本
- 支持添加 / 移除卡牌
- 战斗中实例化为 CardInstance
- 效果通过 Action/Resolver 执行

---

##  遗物系统

- 数据驱动 RelicDefinition
- 运行时 RelicInstance
- 事件监听机制
- 可扩展触发器设计

---

##  药水系统

- 3 槽位限制
- 战斗中使用
- 支持目标选择模式
- 不消耗能量
- 与 Status 系统兼容

---

##  商店系统

- 金币系统
- 随机卡牌 / 遗物 / 药水
- 移除牌服务
- 价格区间可配置
- 所有随机基于 runSeed

---

##  篝火

- Rest：恢复生命
- Upgrade：升级卡牌
- 升级状态可持久化

---

##  Boss 系统

- 独立行为模式
- Boss 专属奖励（3 选 1）
- Victory Summary
- Restart Run 完整重置

---

##  UI 架构

- 统一 UIRoot + PanelManager
- 常驻 HUD（HP / Gold / Floor / Relics / Potions）
- Tooltip 系统
- Targeting 模式支持 ESC 取消
- 地图与战斗 UI 解耦

---

##  可复现性设计

本项目所有随机行为（地图、奖励、商店、Boss 掉落）：

- 基于 runSeed
- 不使用 UnityEngine.Random 全局状态
- 支持固定 seed 回放
- 可用于 Debug 与 Balance 测试

---

##  当前完成度

| 模块 | 状态 |
|------|------|
| 地图 | ✅ 完成 |
| 战斗 | ✅ MVP |
| Buff/Debuff | ✅ |
| 奖励系统 | ✅ |
| 遗物 | ✅ 基础 |
| 药水 | ✅ |
| 事件 | ✅ 基础 |
| 商店 | ✅ |
| 篝火 | ✅ |
| Boss | ✅ |
| UI 优化 | 🚧 进行中 |

---

##  后续规划

- 更多卡牌
- 更多遗物
- 精英与 Boss 行为升级
- 稀有度与权重系统
- 更完善的 UI 动画
- 存档系统
- 难度等级

---

##  技术栈

- Unity 2022.3 LTS
- C#
- ScriptableObject 数据驱动
- 事件总线架构
- asmdef 模块化工程结构

---

仅用于学习与展示用途。
