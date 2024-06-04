import { AppComponent } from './app.component';
import { RegisterComponent } from './views/register/register.component';
import { LoginComponent } from './views/login/login.component';
import { HomeComponent } from './views/home/home.component';
import { ContainersComponent } from './views/containers/containers.component';

import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations'
import { AppRoutingModule } from './app-routing.module';
import { MenubarModule } from 'primeng/menubar';
import { FormsModule } from '@angular/forms';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import {TableModule } from 'primeng/table';

import { AuthService } from './services/auth.service';
import { AuthGuard } from './guards/auth.guard';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './interceptors/interceptor.interceptor';


@NgModule({
  declarations: [
    AppComponent,
    RegisterComponent,
    LoginComponent,
    HomeComponent,
    ContainersComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    ButtonModule,
    MenubarModule,
    FormsModule,
    ProgressSpinnerModule,
    ToastModule,
    TableModule
  ],
  providers: [
    MessageService,
    AuthService,
    AuthGuard,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
