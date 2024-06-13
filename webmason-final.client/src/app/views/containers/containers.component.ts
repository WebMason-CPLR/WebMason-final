import { Component, OnInit } from '@angular/core';
import { ServerService } from '../../services/server.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-containers',
  templateUrl: './containers.component.html',
  styleUrls: ['./containers.component.scss']
})
export class ContainersComponent implements OnInit {
  containers: any[] = [];
  loading: boolean = false;

  constructor(
    private serverService: ServerService,
    private messageService: MessageService
  ) { }

  ngOnInit(): void {
    this.loadContainers();
  }

  loadContainers(): void {
    this.loading = true;
    this.serverService.getUserContainers().subscribe(
      data => {
        console.log(data);
        this.containers = data;
        this.loading = false;
      },
      error => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Error loading containers' });
        this.loading = false;
      }
    );
  }

  deleteContainer(containerId: string): void {
    this.loading = true;

    console.log(containerId);

    this.serverService.deleteContainer(containerId).subscribe(
      response => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Container deleted successfully' });
        this.loadContainers();
        this.loading = false; 
      },
      error => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Error deleting container' });
        this.loading = false;
      }
    );
  }

  getContainerId(container: any): string {
    switch (container.serverType) {
      case 'WordPress':
        return container.id;
        /*return container.wordpresscontainerid;*/
      case 'Odoo':
        return container.id;
        //return container.odoocontainerid;
      case 'Redmine':
        return container.id;
      //return container.redminecontainerid;
      default:
        return '';
    }
  }

}
