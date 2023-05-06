import { Component, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-logout',
  templateUrl: './logout.component.html',
  styleUrls: ['./logout.component.scss']
})
export class LogoutComponent {
  constructor(
    private readonly router: Router,
    private readonly service: AuthService,
    private readonly ngZone: NgZone) { }

  public logout(): void {
    this.service.signOutExternal();
    this.ngZone.run(() => {
      this.router.navigate(['/']).then(() => window.location.reload());
    })
  }
}
