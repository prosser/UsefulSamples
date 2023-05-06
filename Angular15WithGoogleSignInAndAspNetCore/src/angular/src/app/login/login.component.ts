import { Component, NgZone, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CredentialResponse, PromptMomentNotification } from 'google-one-tap';
import { AuthService } from '../services/auth.service';
import { environment } from 'src/environments/environment';
import { accounts } from 'google-one-tap';

// extend the Window interface
interface WindowWithGoogle extends Window {
  onGoogleLibraryLoad: () => void;
  google: { accounts: accounts };
}

declare const window: WindowWithGoogle;

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  constructor(
    private readonly router: Router,
    private readonly ngZone: NgZone,
    private readonly service: AuthService
  ) { }

  ngOnInit(): void {
    window.onGoogleLibraryLoad = () => {
      const a = window.google.accounts;
      const client_id = environment.google.clientId;
      this.ngZone.run(() => {
        a.id.initialize({
          client_id,
          callback: this.handleCredentialResponse.bind(this),
          auto_select: false,
          cancel_on_tap_outside: true,
        });
        const div = document.getElementById('googleButtonDiv');
        if (div) {
          a.id.renderButton(
            div,
            { theme: 'outline', size: 'large' },
          );
        }
        a.id.prompt
      }, this);
    };
  }

  async handleCredentialResponse(response: CredentialResponse) {
    await this.service.loginWithGoogle(response.credential).subscribe(
      x => {
        localStorage.setItem('token', x.token);
        this.ngZone.run(() => {
          // TODO: redirect to target page
          this.router.navigate(['/logout']);
        });
      },
      (error: unknown) => {
        console.error(error);
      }
    );
  }
}
