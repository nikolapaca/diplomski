import { TextFieldModule } from '@angular/cdk/text-field';
import { Component, EventEmitter, Inject, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { UserService } from '../../service/user-service';
import { BoardUpdate } from '../../model/board-update.model';

@Component({
  selector: 'app-update-board-dialog',
  imports: [MatDialogModule,
    MatButtonModule,
    MatInputModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    TextFieldModule],
  templateUrl: './update-board-dialog.html',
  styleUrl: './update-board-dialog.css'
})
export class UpdateBoardDialog implements OnInit{
  public username: string = '';
  public updateBoardForm: FormGroup;
  public errorMessage: string = '';
  @Output() public boardSubmitted = new EventEmitter<BoardUpdate>();

  public constructor(@Inject(MAT_DIALOG_DATA) public data: BoardUpdate, 
                      public dialog: MatDialogRef<UpdateBoardDialog>, 
                      private userService: UserService) {
    this.updateBoardForm = new FormGroup({
      boardName: new FormControl('', [Validators.required, Validators.maxLength(50)]),
      boardDescription: new FormControl('', Validators.maxLength(255))
    });
    this.username = this.userService.getUsername();
  }

  public ngOnInit(): void {
    console.log('vrednost', this.data)
    if (this.data) {
    this.updateBoardForm.patchValue({
      boardName: this.data.oldBoardName,
      boardDescription: this.data.oldBoardDescription
    });
  }
  }

  public setBackendError(message: string): void {
      this.updateBoardForm.get('boardName')?.setErrors({
        'backendError': message
      });
      this.errorMessage = message;
    }
  
  public onSubmit(): void {
      const board: BoardUpdate = {
        oldBoardDescription: this.data.oldBoardDescription,
        newBoardDescription: this.updateBoardForm.value.boardDescription,
        oldBoardName: this.data.oldBoardName,
        newBoardName: this.updateBoardForm.value.boardName,
        ownerUsername: this.data.ownerUsername
      }
      this.boardSubmitted.emit(board);
  }
}
