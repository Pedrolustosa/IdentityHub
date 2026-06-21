/**
 * IdentityHub Design System - Component & Pattern Reference
 * 
 * This file documents all components and utilities available.
 * For updates and new components, see /memories/repo/identityhub-notes.md
 */

// ============================================================================
// DESIGN TOKENS
// ============================================================================
// 
// Colors (extended in tailwind.config.js):
// - primary: Main action color (blue)
// - danger: Destructive actions (red)
// - warning: Caution/alerts (amber)
// - success: Confirmations (green)
// - surface: Backgrounds/neutrals (gray)
// - muted: Secondary/disabled text
// 
// Usage: <div class="bg-primary-600 text-white">
// 
// Spacing Scale:
// xs: 0.25rem (4px)
// sm: 0.5rem (8px)
// md: 1rem (16px)
// lg: 1.5rem (24px)
// xl: 2rem (32px)
// 2xl: 3rem (48px)
//
// Font Sizes:
// xs, sm, base, lg, xl, 2xl (with line-height adjustments)
//

// ============================================================================
// PHASE 1: MVP COMPONENTS
// ============================================================================

// STATUS BADGE
// Usage: <app-status-badge [status]="'active'" label="Active" />
// Available statuses: active | inactive | open | closed | success | error | warning | pending
// Example:
// <app-status-badge [status]="user.isActive ? 'active' : 'inactive'" />

// EMPTY STATE
// Usage: <app-empty-state title="No users" description="..." [actionLabel]="'Create'" [actionRoute]="'/app/users/create'" />
// Shows centered card with icon, title, description, and optional CTA button
// Example:
// <app-empty-state 
//   icon="📭" 
//   title="No sessions found" 
//   description="This user has no active sessions."
//   actionLabel="Back"
//   [actionRoute]="'/app/sessions'"
// />

// COPY TO CLIPBOARD
// Usage: <app-copy-to-clipboard [value]="userId" [truncate]="12" />
// Shows value with copy button. Icon changes to checkmark on successful copy.
// Example:
// <app-copy-to-clipboard [value]="session.id" [truncate]="8" />

// BUTTON UTILITIES
// CSS classes in styles.css
// Usage: <button class="btn btn-primary btn-md">Click me</button>
// Variants: btn-primary | btn-secondary | btn-danger | btn-ghost | btn-link
// Sizes: btn-sm | btn-md (default) | btn-lg
// States: hover, active, disabled, focus-visible
// Example:
// <button class="btn btn-primary btn-lg">Save</button>
// <button class="btn btn-danger btn-md" (click)="delete()">Delete</button>
// <button class="btn btn-secondary btn-sm">Cancel</button>
// <a [routerLink]="'/app/users'" class="btn btn-link">Back to users</a>

// ============================================================================
// PHASE 2: MEDIUM PRIORITY COMPONENTS
// ============================================================================

// PAGE HEADER
// Usage: <app-page-header 
//          title="Users" 
//          subtitle="Manage system users" 
//          [breadcrumbs]="breadcrumbs"
//          actionLabel="Create User"
//          [actionRoute]="'/app/users/create'"
// />
// Props:
// - title: Page heading
// - subtitle: Optional secondary text
// - breadcrumbs: Array of {label, route?} objects
// - actionLabel: CTA button label
// - actionRoute: Navigation target
// - actionIcon: Optional emoji/icon before label
// Example:
// breadcrumbs = [
//   { label: 'Dashboard', route: '/app/dashboard' },
//   { label: 'Users' }
// ]

// SKELETON CARD
// Usage: <app-skeleton-card [showHeader]="true" [showFooter]="true" />
// Shows animated placeholder while loading data.
// Props:
// - showHeader: Show skeleton for header section (default: true)
// - showFooter: Show skeleton for footer/buttons (default: true)
// Example:
// <app-skeleton-card *ngIf="isLoading; else content" />
// <ng-template #content>
//   <!-- Real content -->
// </ng-template>

// FILTER BAR
// Usage: <app-filter-bar 
//          [filterFields]="filters"
//          (filterChange)="onFilterChange($event)"
//          (search)="onSearch($event)"
// />
// FilterField interface:
// {
//   name: string;
//   label: string;
//   type: 'text' | 'select' | 'date' | 'checkbox';
//   placeholder?: string;
//   options?: [{label, value}];
// }
// Example:
// filters: FilterField[] = [
//   { name: 'status', label: 'Status', type: 'select', options: [{label: 'Active', value: 'active'}] },
//   { name: 'role', label: 'Role', type: 'select', options: [{label: 'Admin', value: 'admin'}] },
//   { name: 'createdFrom', label: 'Created From', type: 'date' }
// ];

// DATA TABLE
// Usage: <app-data-table 
//          [columns]="columns"
//          [rows]="users"
//          [pagination]="pagination"
//          [sortable]="true"
//          (sortChange)="onSort($event)"
//          (pageChange)="onPageChange($event)"
// />
// TableColumn interface:
// {
//   key: string;
//   label: string;
//   sortable?: boolean;
//   width?: string;  // e.g., '100px', '20%'
// }
// PaginationState interface:
// {
//   pageIndex: number;
//   pageSize: number;
//   total: number;
// }
// Example:
// columns: TableColumn[] = [
//   { key: 'name', label: 'Name', sortable: true },
//   { key: 'email', label: 'Email', sortable: true },
//   { key: 'status', label: 'Status', sortable: false }
// ];
// pagination = { pageIndex: 0, pageSize: 10, total: 45 };

// ============================================================================
// SERVICES
// ============================================================================

// CONFIRMATION DIALOG SERVICE
// Usage: constructor(private confirmDialog: ConfirmationDialogService) {}
// Methods:
// - confirm(options): Promise<boolean>
// - confirmDelete(itemName): Promise<boolean> (convenience method)
// Example:
// if (await this.confirmDialog.confirmDelete('this user')) {
//   // Delete user
// }

// ============================================================================
// PIPES
// ============================================================================

// FORMAT DATE PIPE
// Usage: {{ date | formatDate }}  or  {{ date | formatDate:'format' }}
// Formats: 'default' | 'long' | 'short' | 'time' | 'relative'
// Examples:
// {{ user.createdAt | formatDate }}                  // "Jun 21, 2026"
// {{ user.createdAt | formatDate:'long' }}           // "June 21, 2026 at 3:45 PM"
// {{ user.createdAt | formatDate:'short' }}          // "6/21/26"
// {{ user.createdAt | formatDate:'time' }}           // "3:45 PM"
// {{ user.createdAt | formatDate:'relative' }}       // "2 hours ago" or "in 3 days"

// ============================================================================
// COMMON PATTERNS
// ============================================================================

// LOADING STATE
// <div class="rounded-xl border bg-white p-6 shadow-sm">
//   <div class="h-8 w-64 animate-pulse rounded bg-slate-100"></div>
// </div>

// ERROR STATE (use LoadErrorBannerComponent or StatusBadge + message)
// <app-load-error-banner [error]="loadError" (retry)="load()" />

// SUCCESS NOTIFICATION
// Use ngx-toastr:
// this.toastr.success('Action completed successfully', 'Success');

// WARNING NOTIFICATION
// this.toastr.warning('Please review this action', 'Warning');

// ============================================================================
// NEXT PHASE (Fase 3) - Polish & Advanced
// ============================================================================

// MODAL COMPONENT
// Usage: <app-modal 
//          [isOpen]="showModal"
//          title="Delete User"
//          body="Are you sure?"
//          [actions]="actions"
//          (closed)="showModal = false"
//          (actionClicked)="handleAction($event)"
// />
// Props:
// - isOpen: boolean - Controls modal visibility
// - title: string - Modal heading
// - body: string - Simple text content (alternative to content template)
// - content: TemplateRef - Complex template content
// - footer: TemplateRef - Custom footer (alternative to actions)
// - showFooter: boolean - Show footer section
// - closeOnBackdrop: boolean - Close when clicking outside (default: true)
// - actions: ModalAction[] - Array of button actions
// ModalAction interface:
// { label: string; variant?: 'primary' | 'danger' | 'secondary'; closeOnClick?: boolean }
// Events:
// - closed: Emitted when modal closes
// - actionClicked: Emitted when action button clicked

// KPI CARD COMPONENT
// Usage: <app-kpi-card [metric]="metric" />
// KpiMetric interface:
// {
//   label: string;
//   value: number | string;
//   unit?: string;
//   icon?: string;
//   color?: 'primary' | 'success' | 'warning' | 'danger';
//   trend?: { value: number; direction: 'up' | 'down' | 'neutral'; period?: string }
// }
// Example:
// metric: KpiMetric = {
//   label: 'Active Users',
//   value: 1234,
//   unit: 'users',
//   icon: '👥',
//   color: 'primary',
//   trend: { value: 12, direction: 'up', period: 'vs last month' }
// };
// <app-kpi-card [metric]="metric" />

// PERMISSION CHIP COMPONENT
// Usage: <app-permission-chip 
//          [permissions]="permissions"
//          [removable]="true"
//          (permissionRemoved)="onRemove($event)"
// />
// Permission interface: { name: string; icon?: string; removable?: boolean }
// Example:
// permissions: Permission[] = [
//   { name: 'users.create', icon: '✓' },
//   { name: 'users.delete', icon: '✗' },
//   { name: 'audit.read', icon: '📋' }
// ];

// DATE RANGE PICKER COMPONENT
// Usage: <app-date-range-picker
//          [showPresets]="true"
//          (rangeChange)="onRangeChange($event)"
// />
// DateRange interface: { startDate: string | null; endDate: string | null }
// Built-in presets: Today, Yesterday, Last 7 days, Last 30 days, This month
// Example:
// onRangeChange(range: DateRange) {
//   console.log(`From ${range.startDate} to ${range.endDate}`);
// }

// TREE VIEW COMPONENT
// Usage: <app-tree-view
//          [nodes]="nodes"
//          (nodeExpanded)="onExpand($event)"
//          (nodeSelected)="onSelect($event)"
// />
// TreeNode interface:
// {
//   id: string | number;
//   label: string;
//   children?: TreeNode[];
//   expanded?: boolean;
//   icon?: string;
//   selectable?: boolean;
//   selected?: boolean;
// }
// Example:
// nodes: TreeNode[] = [
//   {
//     id: 1,
//     label: 'Roles',
//     icon: '👥',
//     expanded: true,
//     children: [
//       { id: 2, label: 'Admin', selectable: true },
//       { id: 3, label: 'User', selectable: true }
//     ]
//   }
// ];

// ============================================================================
// DIRECTIVES
// ============================================================================

// TOOLTIP DIRECTIVE
// Usage: <button [appTooltip]="'Click to save'" [tooltipPosition]="'top'">Save</button>
// Properties:
// - appTooltip: string (required) - Tooltip text
// - tooltipPosition: 'top' | 'bottom' | 'left' | 'right' (default: 'top')
// - tooltipDelay: number (default: 200ms) - Show delay
// Example:
// <input [appTooltip]="'Enter user email'" tooltipPosition="right" />
// <button [appTooltip]="'Delete this item'" tooltipPosition="bottom" [tooltipDelay]="100">Delete</button>

// ============================================================================
// ADVANCED PIPES
// ============================================================================

// SEARCH HIGHLIGHT PIPE
// Usage: {{ text | searchHighlight:searchTerm }}
// Highlights matching terms with yellow background.
// Example:
// {{ user.name | searchHighlight:searchQuery }}
// "John Doe" with searchQuery="john" → "John<mark>john</mark> Doe"
// Note: Use [innerHTML] in component to render HTML:
// <div [innerHTML]="user.name | searchHighlight:searchQuery"></div>
