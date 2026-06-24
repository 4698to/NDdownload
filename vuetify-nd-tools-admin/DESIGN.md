---
name: 天晴动作组工具集管理端
description: 友好清晰的 NDTools 内网资源管理界面，基于 Vuetify Material
colors:
  primary: "#1976D2"
  primary-dark: "#90CAF9"
  secondary: "#424242"
  background: "#FFFFFF"
  background-dark: "#121212"
  surface: "#FFFFFF"
  surface-dark: "#1E1E1E"
  success: "#4CAF50"
  warning: "#FB8C00"
  error: "#FF5252"
  info: "#2196F3"
  quick-install: "#bee0fe"
  standard-install: "#f1e158"
  on-surface-muted: "#757575"
typography:
  display:
    fontFamily: "system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif"
    fontSize: "1.25rem"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "normal"
  title:
    fontFamily: "system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif"
    fontSize: "1rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "normal"
  body:
    fontFamily: "system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
  label:
    fontFamily: "system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif"
    fontSize: "0.75rem"
    fontWeight: 500
    lineHeight: 1.4
    letterSpacing: "normal"
rounded:
  sm: "4px"
  md: "8px"
  lg: "12px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "#FFFFFF"
    rounded: "{rounded.md}"
    padding: "0 16px"
  button-tonal:
    backgroundColor: "rgba(25, 118, 210, 0.12)"
    textColor: "{colors.primary}"
    rounded: "{rounded.md}"
    padding: "0 12px"
  chip-status:
    backgroundColor: "rgba(33, 150, 243, 0.12)"
    textColor: "{colors.info}"
    rounded: "{rounded.sm}"
    padding: "0 8px"
---

# Design System: 天晴动作组工具集管理端

## 1. Overview

**Creative North Star: "清晰工位"（The Friendly Workbench）**

这是一套面向内网资源浏览与维护的产品界面：公众可以像查阅工具目录一样浏览安装清单，少数编辑者在同一视觉语言下完成打包与发布。设计在 Vuetify Material 3 之上做克制优化——熟悉、可信、不压迫，用分组与状态色传达层次，而不是装饰。

系统明确拒绝 PRODUCT.md 中的反例：密密麻麻的无层次表格墙、一屏堆满 alert/chip/按钮、以及牺牲可读性的「好看」装饰。动效仅用于状态反馈，尊重 `prefers-reduced-motion`。

**Key Characteristics:**

- **Restrained 配色**：蓝色主色仅用于主操作与当前选中，语义色（success/warning/error/info）用于状态
- **系统字体**：Windows 微软雅黑 / macOS 苹方，无 Web 字体加载
- **紧凑但不拥挤**：管理页 `density="compact"`，对话框 `comfortable`，侧栏 + 主工作区分明
- **诚实的状态反馈**：未保存、缺失 zip、加载中、鉴权失败均用 tonal alert/chip 直白呈现
- **深浅色双主题**：`defaultTheme: system'`，亮/暗各有一套 surface 与 primary 变体

## 2. Colors

整体为 **Restrained Material Blue** 策略：白/深灰表面 + 单一蓝色主色 + 完整语义色板。领域专用色 `quick` / `noquick` 仅用于安装类型标识。

### Primary

- **晴工具蓝 (Material Blue)** (`#1976D2` 亮色 / `#90CAF9` 暗色)：主按钮（保存、发布、输入密钥）、侧栏激活项、关键 CTA。暗色模式下 primary 提亮以保证对比度。

### Secondary

- **中性灰 (Secondary Gray)** (`#424242`)：次要文本、非强调图标；两主题共用。

### Neutral

- **画布白 (Canvas White)** (`#FFFFFF`)：亮色背景与卡片表面
- **深墨底 (Ink Dark)** (`#121212`)：暗色背景
- **浮层灰 (Elevated Surface)** (`#1E1E1E`)：暗色卡片/面板表面
- **辅助文字 (Muted)** (`#757575` 近似 `text-medium-emphasis`)：副标题、caption、表格次要列

### Semantic

- **成功绿** (`#4CAF50`)：资源包存在、操作成功
- **警示橙** (`#FB8C00`)：未保存、有变更、警告类 alert
- **错误红** (`#FF5252`)：删除、鉴权失败、缺失文件
- **信息蓝** (`#2196F3`)：提示、发布预览 chip、信息 alert

### Domain

- **快速安装蓝** (`#bee0fe`)：`quick` 类型资源 chip 背景
- **标准安装黄** (`#f1e158`)：非 quick 资源 chip 背景

### Named Rules

**The One Accent Rule.** 主色蓝仅用于可点击的主操作与当前导航选中。禁止用主色大面积铺底或装饰性描边。

**The Semantic-Only Rule.** success/warning/error/info 只表达状态，不用于品牌装饰。

## 3. Typography

**Display / Title / Body / Label Font:** 统一系统字体栈  
`system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif`

**Character:** 单一无衬线家族贯穿全局；中文界面优先保证 Windows/macOS 原生可读性，不引入 Web 字体加载。层级靠字重与 Vuetify 预设尺寸（`text-h6`、`text-subtitle-1`、`text-caption`）区分，而非夸张字号对比。

### Hierarchy

- **Display** (600, 1.25rem / `text-h6`, line-height 1.3)：页面标题，如「天晴安装器资源包」
- **Title** (600, 1rem / `text-subtitle-1`, line-height 1.4)：区块标题、侧栏「数据管理」、卡片标题
- **Body** (400, 0.875rem / `text-body-2`, line-height 1.5)：表格内容、表单标签、树节点说明；长说明文本控制在 65–75ch
- **Label** (500, 0.75rem / `text-caption`, line-height 1.4)：副标题、时间戳、状态摘要 chip 内文字

### Named Rules

**The System Font Rule.** 禁止引入 Roboto 或其他 Web 字体作为正文字体；保持系统栈。

**The Density Type Rule.** 管理表格与侧栏用 compact；对话框表单用 comfortable。同一屏幕不混用两种密度。

## 4. Elevation

本系统以 **tonal layering（色调分层）** 为主，阴影极少。深度通过 `v-sheet` 边框（`border="e"`）、`variant="tonal"` 的 alert/chip 背景色，以及亮色模式下 app bar 的 `elevation-4` 传达。

### Shadow Vocabulary

- **App Bar** (`elevation-4`，Vuetify 默认 ≈ `0 2px 4px rgba(0,0,0,0.14)`）：仅顶栏使用，分隔导航与内容
- **其余表面**：默认无阴影；卡片用 flat sheet + 边框，不用 ghost-card（边框 + 大阴影叠加）

### Named Rules

**The Flat Surface Rule.** 卡片、表格容器、侧栏默认无 drop shadow。需要分隔时用 `border` 或 tonal 背景，不用 16px+ 模糊阴影。

**The No Ghost Card Rule.** 禁止 `1px border` 与 `box-shadow blur ≥16px` 同时出现在同一元素上。

## 5. Components

### Buttons

- **Shape:** 中等圆角（Vuetify 默认 ≈ 4px，`rounded="lg"` 用于列表项为 12px）
- **Primary (`color="primary"`):** 保存、发布、输入密钥；块级按钮在侧栏 `block`
- **Tonal (`variant="tonal"`):** 次要操作：重新加载、新建、编辑、检查资源包；低对比背景 + 主色文字
- **Text (`variant="text"`):** 折叠/展开、取消、返回；无背景
- **Flat (`variant="flat"`):** 发布主按钮等需要更强视觉权重时
- **Hover / Focus:** 依赖 Vuetify 内置状态；不添加额外动画

### Chips

- **Status (`variant="tonal"`, `size="small"`):** 全局状态栏（发布预览、资源包检查、错误/成功消息）
- **Domain (`variant="flat"`):** 安装类型 quick/noquick，使用 `quick` / `noquick` 自定义色
- **Semantic:** success/warning/error/info 色 + tonal 变体

### Cards / Containers

- **Corner Style:** 默认直角卡片；列表项 `rounded="lg"`（12px）
- **Background:** 与 `surface` 同色；侧栏 `v-sheet` + 右边框
- **Shadow Strategy:** 无；见 Elevation
- **Border:** `border="e"` 用于侧栏、工作区编辑器 `v-sheet`
- **Internal Padding:** 页面 `pa-4`；侧栏操作区 `pa-3`；表单项 `mb-3`

### Inputs / Fields

- **Style:** `variant="outlined"`, `density="compact"`（管理页）/ `comfortable`（对话框）
- **Focus:** Vuetify 默认主色描边
- **Readonly:** 时间戳、sha、LastPack 等运维字段只读展示

### Navigation

- **Top:** `v-app-bar` flat + elevation-4；`v-tabs` compact 切换三个资源视图
- **Admin side nav:** 240px `v-sheet`，`v-list` nav compact，`rounded="lg"` 列表项
- **Active state:** `active` + primary 色调；返回首页用 text 按钮

### Data Table / Tree

- **Table:** `v-data-table` compact, fixed-header, 50 行分页；选中行 `row-selected` 主色 14% 背景
- **Tree:** `v-treeview` / 自定义树形表格，文件夹/文件图标 + badge 子项数量

### Dialogs

- **Standard:** `max-width="480–720"`, `persistent` 用于密钥与确认
- **Fullscreen:** JSON 编辑器 `JsonEditDialog` 全屏 + primary toolbar

## 6. Do's and Don'ts

### Do:

- **Do** 用 `variant="tonal"` 的 chip/alert 传达状态，保持主内容区留白
- **Do** 危险操作（删除、发布）前用确认对话框
- **Do** 管理页统一 `density="compact"`，对话框 `comfortable`
- **Do** 尊重 `prefers-reduced-motion`，过渡限于 150–250ms 状态反馈
- **Do** 侧栏 + 主工作区 + 可选右侧编辑面板的三栏布局（installbox-edit）

### Don't:

- **Don't** 做成密密麻麻的企业后台：无层次、无呼吸感的信息墙（PRODUCT.md 反例）
- **Don't** 一屏堆满 alert/chip/按钮、缺乏主次
- **Don't** 使用渐变文字、玻璃拟态卡片墙、无意义动效等装饰性 UI
- **Don't** 用 `border-left` 粗色条作为卡片装饰
- **Don't** 在 inactive 状态使用高饱和主色
- **Don't** 引入 Web 字体导致打包体积膨胀与 preload 泛滥
