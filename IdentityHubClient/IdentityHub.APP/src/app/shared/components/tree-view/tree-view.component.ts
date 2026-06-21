import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TreeNode {
  id: string | number;
  label: string;
  children?: TreeNode[];
  expanded?: boolean;
  icon?: string;
  selectable?: boolean;
  selected?: boolean;
}

@Component({
  selector: 'app-tree-view',
  standalone: true,
  imports: [CommonModule, forwardRef(() => TreeNodeComponent)],
  template: `
    <div class="rounded-lg border border-slate-200 bg-white shadow-sm">
      <div class="max-h-96 overflow-y-auto">
        @for (node of nodes; track node.id) {
          <app-tree-node
            [node]="node"
            (toggleExpand)="onToggleExpand($event)"
            (nodeSelect)="onNodeSelect($event)"
          />
        }
      </div>
    </div>
  `
})
export class TreeViewComponent {
  @Input() nodes: TreeNode[] = [];
  @Output() nodeExpanded = new EventEmitter<TreeNode>();
  @Output() nodeSelected = new EventEmitter<TreeNode>();

  onToggleExpand(node: TreeNode): void {
    node.expanded = !node.expanded;
    this.nodeExpanded.emit(node);
  }

  onNodeSelect(node: TreeNode): void {
    if (node.selectable !== false) {
      node.selected = !node.selected;
      this.nodeSelected.emit(node);
    }
  }
}

@Component({
  selector: 'app-tree-node',
  standalone: true,
  imports: [CommonModule, forwardRef(() => TreeNodeComponent)],
  template: `
    <div class="border-b border-slate-100 last:border-b-0">
      <div
        class="flex items-center gap-2 px-4 py-3 hover:bg-slate-50 cursor-pointer transition-colors"
        [class.bg-primary-50]="node.selected"
      >
        @if (node.children && node.children.length > 0) {
          <button
            type="button"
            (click)="onToggleExpand()"
            class="inline-flex items-center justify-center rounded p-0.5 hover:bg-slate-200 transition-colors"
          >
            <svg
              class="h-4 w-4 text-slate-600 transition-transform"
              [class.rotate-90]="node.expanded"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        } @else {
          <div class="w-6"></div>
        }

        @if (node.selectable !== false) {
          <input
            type="checkbox"
            [checked]="node.selected"
            (change)="onSelect()"
            class="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
          />
        }

        @if (node.icon) {
          <span class="text-base">{{ node.icon }}</span>
        }

        <span class="text-sm font-medium text-slate-900">{{ node.label }}</span>
      </div>

      @if (node.expanded && node.children && node.children.length > 0) {
        <div class="border-l-2 border-slate-200">
          @for (child of node.children; track child.id) {
            <app-tree-node
              [node]="child"
              (toggleExpand)="onChildExpand($event)"
              (nodeSelect)="onChildSelect($event)"
            />
          }
        </div>
      }
    </div>
  `
})
export class TreeNodeComponent {
  @Input() node!: TreeNode;
  @Output() toggleExpand = new EventEmitter<TreeNode>();
  @Output() nodeSelect = new EventEmitter<TreeNode>();

  onToggleExpand(): void {
    this.toggleExpand.emit(this.node);
  }

  onSelect(): void {
    this.nodeSelect.emit(this.node);
  }

  onChildExpand(node: TreeNode): void {
    this.toggleExpand.emit(node);
  }

  onChildSelect(node: TreeNode): void {
    this.nodeSelect.emit(node);
  }
}
